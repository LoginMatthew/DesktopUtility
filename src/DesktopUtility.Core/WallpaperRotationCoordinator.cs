namespace DesktopUtility.Core;

public enum WallpaperActionResult
{
    Skipped,
    Applied,
    Failed,
    AccessDenied
}

public sealed class WallpaperRotationCoordinator(IWallpaperService wallpaper, TimeProvider clock)
{
    private readonly RotationSchedule schedule = new(clock);
    private readonly WallpaperRotation rotation = new(wallpaper);
    private readonly object stateGate = new();
    private readonly SemaphoreSlim actionGate = new(1);
    private IReadOnlyList<string> candidates = [];
    private IReadOnlyDictionary<string, IReadOnlyList<string>> pools = new Dictionary<string, IReadOnlyList<string>>();
    private int pendingActions;
    private bool enabled;
    private bool separateMonitors;
    private int intervalMinutes;

    public TimeSpan? Remaining
    {
        get
        {
            lock (stateGate)
            {

                return schedule.Remaining;
            }
        }
    }

    public void Configure(
        IReadOnlyList<string> images,
        IReadOnlySet<string> selected,
        bool selectedOnly,
        bool enabled,
        int intervalMinutes,
        bool separateMonitors,
        IEnumerable<MonitorWallpaperPool>? monitorPools = null)
    {
        var updatedCandidates = selectedOnly ? images.Where(selected.Contains).ToArray() : images.ToArray();
        var updatedPools = (monitorPools ?? []).Where(pool => pool.Images.Count > 0).ToDictionary(
            pool => pool.MonitorId,
            pool => (IReadOnlyList<string>)pool.Images.ToArray(),
            StringComparer.OrdinalIgnoreCase);
        lock (stateGate)
        {
            candidates = updatedCandidates;
            pools = updatedPools;
            this.enabled = enabled;
            this.intervalMinutes = intervalMinutes;
            this.separateMonitors = separateMonitors;
            RestartSchedule();
        }
    }

    public Task<WallpaperActionResult> TickAsync()
    {
        IReadOnlyList<string> currentCandidates;
        bool useSeparateMonitors;
        IReadOnlyDictionary<string, IReadOnlyList<string>> currentPools;
        lock (stateGate)
        {

            if (!schedule.IsDue || pendingActions > 0)
            {

                return Task.FromResult(WallpaperActionResult.Skipped);
            }

            currentCandidates = candidates;
            useSeparateMonitors = separateMonitors;
            currentPools = pools;
            BeginAction();
        }

        return RunAsync(() => rotation.AdvanceAsync(currentCandidates, useSeparateMonitors, currentPools));
    }

    public Task<WallpaperActionResult> ApplyAsync(string path, string? monitorId)
    {
        lock (stateGate)
        {
            BeginAction();
        }

        return RunAsync(() => rotation.ApplyAsync(path, monitorId));
    }

    public Task<WallpaperActionResult> ApplyRandomAsync(string monitorId)
    {
        IReadOnlyList<string> pool;
        lock (stateGate)
        {

            if (!pools.TryGetValue(monitorId, out var configuredPool))
            {

                return Task.FromResult(WallpaperActionResult.Skipped);
            }

            pool = configuredPool;
            BeginAction();
        }

        return RunAsync(() => rotation.ApplyRandomAsync(pool, monitorId));
    }

    public Task<WallpaperActionResult> ApplyAssignmentsAsync(IReadOnlyList<MonitorWallpaperAssignment> assignments)
    {

        if (assignments.Count == 0)
        {

            return Task.FromResult(WallpaperActionResult.Skipped);
        }

        var snapshot = assignments.ToArray();
        lock (stateGate)
        {
            BeginAction();
        }

        return RunAsync(() => rotation.ApplyAssignmentsAsync(snapshot));
    }

    private void BeginAction()
    {
        pendingActions++;
        RestartSchedule();
    }

    private async Task<WallpaperActionResult> RunAsync(Func<Task<WallpaperActionResult>> action)
    {
        await actionGate.WaitAsync().ConfigureAwait(false);
        try
        {

            return await action().ConfigureAwait(false);
        }
        catch (WallpaperAccessDeniedException)
        {

            return WallpaperActionResult.AccessDenied;
        }
        catch (Exception error) when (error is WallpaperOperationException or OperationCanceledException)
        {

            return WallpaperActionResult.Failed;
        }
        finally
        {
            lock (stateGate)
            {
                RestartSchedule();
                pendingActions--;
            }
            actionGate.Release();
        }
    }

    private void RestartSchedule()
    {
        bool hasImages = candidates.Count > 0 || (separateMonitors && pools.Count > 0);
        schedule.Restart(enabled, hasImages ? 1 : 0, intervalMinutes);
    }
}
