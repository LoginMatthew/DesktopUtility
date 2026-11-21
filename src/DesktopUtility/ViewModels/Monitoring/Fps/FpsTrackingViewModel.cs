using System.ComponentModel;
using DesktopUtility.Infrastructure.Monitoring.Fps;

namespace DesktopUtility.ViewModels;

internal sealed class FpsTrackingViewModel(Settings settings, Language language, Action save) : INotifyPropertyChanged
{
    private Task previousCaptures = Task.CompletedTask;
    public FpsCapturePool CapturePool { get; } = new(PresentMonCapture.CaptureAsync);
    public Language Words => language;
    public string? ExecutablePath => settings.PresentMonPath;
    public IReadOnlyList<FpsMonitorViewModel> Monitors { get; private set; } = [];
    public bool Allowed { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task<bool> SetExecutableAsync(string path)
    {

        if (!await Task.Run(() => PresentMonCapture.IsSupported(path)))
        {

            return false;
        }

        StopAll();
        settings.PresentMonPath = path;
        save();
        RefreshLanguage();

        return true;
    }

    public void SetAllowed(bool value)
    {

        if (Allowed != value)
        {
            Allowed = value;

            if (!value)
            {
                StopAll();
            }

            RefreshLanguage();
        }
    }

    public void RefreshWindows()
    {
        var previous = Monitors.ToDictionary(monitor => monitor.Id, monitor => monitor.SelectedWindow, StringComparer.OrdinalIgnoreCase);
        StopAll();
        previousCaptures = WaitForCapturesAsync();
        Monitors = DesktopWindowCatalog.Read().Select(monitor =>
        {
            var old = previous.GetValueOrDefault(monitor.Id);
            var selected = monitor.Windows.FirstOrDefault(window => old?.Handle == window.Handle && old.ProcessId == window.ProcessId);

            return new FpsMonitorViewModel(this, monitor, selected);
        }).ToArray();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Monitors)));
    }

    public void RefreshLanguage()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

        foreach (var monitor in Monitors)
        {
            monitor.Refresh();
        }
    }

    private void StopAll()
    {

        foreach (var monitor in Monitors)
        {
            monitor.Stop();
        }
    }

    public Task WaitForCapturesAsync() =>
        Task.WhenAll(Monitors.Select(monitor => monitor.Completion).Append(previousCaptures));
}
