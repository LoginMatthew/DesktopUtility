namespace DesktopUtility.Core;

public enum WallpaperActionResult
{
    Skipped,
    Applied,
    Failed
}

public sealed class WallpaperRotationCoordinator(IWallpaperService wallpaper, TimeProvider clock)
{
    private readonly RotationSchedule schedule = new(clock);
    private readonly WallpaperRotation rotation = new(wallpaper);
    private readonly object stateGate = new();
    private readonly SemaphoreSlim actionGate = new(1);
    private IReadOnlyList<string> candidates = [];
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
        bool separateMonitors)
    {
        var updatedCandidates = selectedOnly ? images.Where(selected.Contains).ToArray() : images.ToArray();
        lock (stateGate)
        {
            candidates = updatedCandidates;
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
        lock (stateGate)
        {

            if (!schedule.IsDue || pendingActions > 0)
            {

                return Task.FromResult(WallpaperActionResult.Skipped);
            }

            currentCandidates = candidates;
            useSeparateMonitors = separateMonitors;
            BeginAction();
        }

        return RunAsync(() => rotation.AdvanceAsync(currentCandidates, useSeparateMonitors));
    }

    public Task<WallpaperActionResult> ApplyAsync(string path, string? monitorId)
    {
        lock (stateGate)
        {
            BeginAction();
        }

        return RunAsync(async () =>
        {
            await wallpaper.ApplyAsync(path, monitorId).ConfigureAwait(false);

            return WallpaperActionResult.Applied;
        });
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

    private void RestartSchedule() => schedule.Restart(enabled, candidates.Count, intervalMinutes);
}
