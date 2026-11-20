using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using DesktopUtility.Infrastructure.Wallpaper;

namespace DesktopUtility.Services;

internal sealed class WallpaperSession : IDisposable
{
    private readonly IWallpaperService wallpaper;
    private readonly WallpaperRotation rotation;
    private readonly DispatcherTimer timer;
    private readonly WallpaperWorker worker = new();
    private int pendingActions;
    private bool disposed;
    private readonly RotationSchedule schedule;
    private IReadOnlyList<string> candidates = [];


    public WallpaperSession(Settings settings, IWallpaperService wallpaper, TimeProvider clock)
    {
        Settings = settings;
        this.wallpaper = wallpaper;
        schedule = new RotationSchedule(clock);
        rotation = new WallpaperRotation(wallpaper);
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += OnTick;
        timer.Start();
    }

    public Settings Settings { get; }
    public IReadOnlyList<string> Images { get; private set; } = [];
    public string? MessageKey { get; set; }
    public event Action? Changed;
    public event Action? StateUpdated;
    public event Action? ScheduleChanged;

    public TimeSpan? Remaining => schedule.Remaining;

    public void SetImages(IReadOnlyList<string> images)
    {
        Images = images;
        RefreshCandidates();
    }

    public void RefreshCandidates()
    {
        candidates = Settings.SelectedOnly ? Images.Where(Settings.Selected.Contains).ToArray() : Images;
        ResetSchedule();
    }

    public void ResetSchedule()
    {
        schedule.Restart(Settings.RotationEnabled, candidates.Count, Settings.IntervalMinutes);
        StateUpdated?.Invoke();
    }

    public void NotifyState() => StateUpdated?.Invoke();

    public void UpdateSchedule()
    {
        ResetSchedule();
        ScheduleChanged?.Invoke();
        Changed?.Invoke();
    }

    public void Save() => Changed?.Invoke();

    public async Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync()
    {
        try
        {

            return await worker.RunAsync(wallpaper.GetMonitors);
        }
        catch (COMException)
        {

            return [];
        }
    }

    public void Apply(string? path, string? monitorId)
    {

        if (path is null)
        {
            MessageKey = "Focus";
            StateUpdated?.Invoke();

            return;
        }

        _ = RunWallpaperActionAsync(() => wallpaper.Apply(path, monitorId));
    }

    public void ApplyFavorite()
    {

        if (Settings.Favorite is null)
        {
            MessageKey = "NoFavorite";
            StateUpdated?.Invoke();

            return;
        }

        Apply(Settings.Favorite, null);
    }

    private void OnTick(object? sender, EventArgs args)
    {

        if (schedule.IsDue && pendingActions == 0)
        {
            var currentCandidates = candidates;
            bool separateMonitors = Settings.SeparateMonitors;
            _ = RunWallpaperActionAsync(() => rotation.Advance(currentCandidates, separateMonitors));
        }
        else
        {
            StateUpdated?.Invoke();
        }
    }

    private async Task RunWallpaperActionAsync(Action action)
    {
        pendingActions++;
        ResetSchedule();
        try
        {
            await worker.RunAsync(action);
            MessageKey = null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException or AggregateException or OperationCanceledException)
        {
            MessageKey = "WallpaperError";
        }

        finally
        {
            pendingActions--;

            if (!disposed)
            {
                ResetSchedule();
            }
        }
    }

    public void Dispose()
    {
        disposed = true;
        timer.Stop();
        worker.Dispose();
    }
}
