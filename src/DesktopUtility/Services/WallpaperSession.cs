namespace DesktopUtility.Services;

internal sealed class WallpaperSession(Settings settings, IWallpaperService wallpaper, TimeProvider clock) : IDisposable
{
    private readonly WallpaperRotationCoordinator rotation = new(wallpaper, clock);
    private bool disposed;

    public Settings Settings { get; } = settings;
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

        _ = UpdateAfterActionAsync(rotation.ApplyAsync(path, monitorId));
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

    private async Task UpdateAfterActionAsync(Task<WallpaperActionResult> action)
    {
        var result = await action;

        if (disposed)
        {

            return;
        }

        if (result != WallpaperActionResult.Skipped)
        {
            MessageKey = result == WallpaperActionResult.Failed ? "WallpaperError" : null;
        }

        NotifyState();
    }

    private void RefreshSchedule()
    {
        rotation.Configure(Images, Settings.Selected, Settings.SelectedOnly,
            Settings.RotationEnabled, Settings.IntervalMinutes, Settings.SeparateMonitors);
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
