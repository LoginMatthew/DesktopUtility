namespace DesktopUtility.Services;

internal sealed class WallpaperSession : IDisposable
{
    private readonly IWallpaperService wallpaper;
    private readonly WallpaperRotationCoordinator rotation;
    private bool disposed;

    public WallpaperSession(Settings settings, IWallpaperService wallpaper, TimeProvider clock)
    {
        Settings = settings;
        this.wallpaper = wallpaper;
        rotation = new WallpaperRotationCoordinator(wallpaper, clock);
        RefreshSchedule();
    }

    public Settings Settings { get; }
    public IReadOnlyList<string> Images { get; private set; } = [];
    public string? MessageKey { get; set; }
    public event Action? Changed;
    public event Action? StateUpdated;
    public event Action? ScheduleChanged;

    public TimeSpan? Remaining => rotation.Remaining;

    public void SetFolderImages(string folder, IReadOnlyList<string> images)
    {
        bool folderChanged = !string.Equals(Settings.Folder, folder, StringComparison.OrdinalIgnoreCase);

        if (folderChanged)
        {
            Settings.Folder = folder;
            Settings.Selected.Clear();
        }

        Images = images;
        RefreshSchedule();

        if (folderChanged)
        {
            Save();
        }
    }

    public void SetSelectedOnly(bool value)
    {
        Settings.SelectedOnly = value;
        RefreshSchedule();
        Save();
    }

    public void SetSelected(string path, bool selected)
    {

        if (selected)
        {
            Settings.Selected.Add(path);
        }
        else
        {
            Settings.Selected.Remove(path);
        }

        if (Settings.SelectedOnly)
        {
            RefreshSchedule();
        }

        Save();
    }

    public void SetAllSelected(bool selected)
    {

        if (selected)
        {
            Settings.Selected.UnionWith(Images);
        }
        else
        {
            Settings.Selected.ExceptWith(Images);
        }

        if (Settings.SelectedOnly)
        {
            RefreshSchedule();
        }

        Save();
    }

    public void SetMonitorAssignment(string monitorId, string path)
    {
        Settings.MonitorAssignments[monitorId] = path;
        Save();
    }

    public void ClearMonitorAssignment(string monitorId)
    {

        if (Settings.MonitorAssignments.Remove(monitorId))
        {
            Save();
        }
    }

    public void SetMonitorFavorite(string monitorId, string path)
    {
        Settings.MonitorFavorites[monitorId] = path;
        Save();
    }

    public void ClearMonitorFavorite(string monitorId)
    {

        if (Settings.MonitorFavorites.Remove(monitorId))
        {
            Save();
        }
    }

    public void ChangeMonitorPool(string monitorId, IEnumerable<string> paths, bool include)
    {
        var pool = Settings.MonitorPools.GetValueOrDefault(monitorId) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int previousCount = pool.Count;

        if (include)
        {
            pool.UnionWith(paths);
        }
        else
        {
            pool.ExceptWith(paths);
        }

        if (pool.Count == previousCount)
        {

            return;
        }

        if (pool.Count == 0)
        {
            Settings.MonitorPools.Remove(monitorId);
        }
        else
        {
            Settings.MonitorPools[monitorId] = pool;
        }

        RefreshSchedule();
        Save();
    }

    public void ApplyRandomFromPool(string monitorId) =>
        _ = UpdateAfterActionAsync(rotation.ApplyRandomAsync(monitorId), showConfirmation: true);

    public void ApplyMonitorAssignments(IReadOnlyList<MonitorWallpaperAssignment> assignments) =>
        _ = UpdateAfterActionAsync(rotation.ApplyAssignmentsAsync(assignments), showConfirmation: true);

    public void SetFavorite(string? path)
    {
        Settings.Favorite = path;
        Save();
    }

    public void SetRotationEnabled(bool enabled)
    {
        Settings.RotationEnabled = enabled;
        UpdateSchedule();
    }

    public void SetInterval(int minutes)
    {
        Settings.IntervalMinutes = minutes;
        MessageKey = null;
        UpdateSchedule();
    }

    public void SetSeparateMonitors(bool value)
    {
        Settings.SeparateMonitors = value;
        UpdateSchedule();
    }

    public void NotifyState() => StateUpdated?.Invoke();
    public void Save() => Changed?.Invoke();

    public async Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync()
    {
        try
        {

            return await wallpaper.GetMonitorsAsync();
        }
        catch (WallpaperAccessDeniedException)
        {
            MessageKey = "WallpaperAccessDenied";
            NotifyState();

            return [];
        }
        catch (Exception error) when (error is WallpaperOperationException or OperationCanceledException)
        {

            return [];
        }
    }

    public void Apply(string? path, string? monitorId)
    {

        if (path is null)
        {
            MessageKey = "Focus";
            NotifyState();

            return;
        }

        _ = UpdateAfterActionAsync(rotation.ApplyAsync(path, monitorId), showConfirmation: true);
    }

    public void ApplyFavorite()
    {

        if (Settings.Favorite is null)
        {
            MessageKey = "NoFavorite";
            NotifyState();

            return;
        }

        Apply(Settings.Favorite, null);
    }

    public Task TickAsync() => UpdateAfterActionAsync(rotation.TickAsync());

    private async Task UpdateAfterActionAsync(Task<WallpaperActionResult> action, bool showConfirmation = false)
    {
        var result = await action;

        if (disposed)
        {

            return;
        }

        if (result != WallpaperActionResult.Skipped)
        {
            MessageKey = result switch
            {
                WallpaperActionResult.AccessDenied => "WallpaperAccessDenied",
                WallpaperActionResult.Failed => "WallpaperError",
                _ => showConfirmation ? "WallpaperApplied" : null
            };
        }

        NotifyState();
    }

    private void RefreshSchedule()
    {
        rotation.Configure(Images, Settings.Selected, Settings.SelectedOnly,
            Settings.RotationEnabled, Settings.IntervalMinutes, Settings.SeparateMonitors,
            Settings.MonitorPools.Select(pair => new MonitorWallpaperPool(pair.Key, pair.Value)));
        NotifyState();
    }

    private void UpdateSchedule()
    {
        RefreshSchedule();
        ScheduleChanged?.Invoke();
        Save();
    }

    public void Dispose() => disposed = true;
}
