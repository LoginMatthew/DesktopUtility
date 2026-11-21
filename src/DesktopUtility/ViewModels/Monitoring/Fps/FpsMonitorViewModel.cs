using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using DesktopUtility.Infrastructure.Monitoring.Fps;

namespace DesktopUtility.ViewModels;

internal sealed class FpsMonitorViewModel(FpsTrackingViewModel owner, MonitorWindows monitor, WindowCaptureTarget? selected) : INotifyPropertyChanged
{
    private WindowCaptureTarget? selectedWindow = selected;
    private CancellationTokenSource? currentRun;
    private Task captureTask = Task.CompletedTask;
    private double? framesPerSecond;
    private string statusKey = "MetricOff";

    public string Id => monitor.Id;
    public Task Completion => captureTask;
    public string Name => monitor.Name;
    public IReadOnlyList<WindowCaptureTarget> Windows => monitor.Windows;
    public string ToggleLabel => owner.Words["FpsToggle"];
    public string WindowLabel => owner.Words["FpsWindow"];
    public bool CanCapture => owner.Allowed && selectedWindow is not null && !string.IsNullOrWhiteSpace(owner.ExecutablePath);
    public string Value => framesPerSecond is { } value
        ? $"{value.ToString("0.#", CultureInfo.GetCultureInfo(owner.Words.Code))} FPS"
        : owner.Words[statusKey];

    public WindowCaptureTarget? SelectedWindow
    {
        get => selectedWindow;
        set
        {

            if (selectedWindow != value)
            {
                selectedWindow = value;
                Stop();
            }
        }
    }

    public bool Measuring
    {
        get => currentRun is not null;
        set
        {

            if (value && currentRun is null && CanCapture)
            {
                Start();
            }
            else if (!value && currentRun is not null)
            {
                Stop();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

    public void Stop()
    {
        currentRun?.Cancel();
        currentRun = null;
        framesPerSecond = null;
        statusKey = "MetricOff";
        Refresh();
    }

    private void Start()
    {
        var run = new CancellationTokenSource();
        currentRun = run;
        statusKey = "MetricWaiting";
        captureTask = CaptureAfterAsync(captureTask, owner.ExecutablePath!, selectedWindow!, Dispatcher.CurrentDispatcher, run);
        Refresh();
    }

    private async Task CaptureAfterAsync(Task previous, string executable, WindowCaptureTarget window, Dispatcher dispatcher, CancellationTokenSource run)
    {
        try
        {
            await previous;
            run.Token.ThrowIfCancellationRequested();

            if (!DesktopWindowCatalog.IsCurrent(window))
            {
                throw new InvalidOperationException("The selected window was closed.");
            }

            await owner.CapturePool.SubscribeAsync(executable, window.ProcessId, fps => Publish(dispatcher, run, window, fps), run.Token);

            if (ReferenceEquals(currentRun, run))
            {
                statusKey = "FpsNoFrames";
            }
        }
        catch (OperationCanceledException) when (run.IsCancellationRequested)
        {
            // The user ended capture or the monitoring page was hidden.
        }
        catch (Exception error) when (error is Win32Exception or IOException or InvalidOperationException or UnauthorizedAccessException or ArgumentException)
        {

            if (ReferenceEquals(currentRun, run))
            {
                statusKey = error switch
                {
                    PresentMonVersionException => "FpsInvalidExecutable",
                    UnauthorizedAccessException or Win32Exception { NativeErrorCode: 5 or 740 } => "FpsAccessDenied",
                    _ => "FpsCaptureUnavailable"
                };
            }
        }
        finally
        {

            if (ReferenceEquals(currentRun, run))
            {
                currentRun = null;
                framesPerSecond = null;
                Refresh();
            }

            run.Dispose();
        }
    }

    private void Publish(Dispatcher dispatcher, CancellationTokenSource run, WindowCaptureTarget window, double? fps)
    {
        _ = dispatcher.InvokeAsync(() =>
        {

            if (ReferenceEquals(currentRun, run))
            {

                if (!DesktopWindowCatalog.IsCurrent(window))
                {
                    Stop();

                    return;
                }

                framesPerSecond = fps;
                statusKey = "FpsNoFrames";
                Refresh();
            }
        }, DispatcherPriority.Background);
    }
}
