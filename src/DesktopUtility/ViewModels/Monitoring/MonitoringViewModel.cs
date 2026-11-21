using System.ComponentModel;
using System.Windows.Threading;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.ViewModels;

internal sealed class MonitoringViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly Settings settings;
    private readonly MonitoringSampler sampler;
    private readonly Action save;
    private readonly Dispatcher dispatcher;
    private readonly Action<SystemMetric>? setTemperatureMetrics;
    private CancellationTokenSource? currentRun;
    private Task currentTask = Task.CompletedTask;
    private bool viewing;
    private bool disposed;

    public MonitoringViewModel(Settings settings, Language language, MonitoringSampler sampler, Action save, Dispatcher dispatcher,
        Action<SystemMetric>? setTemperatureMetrics = null)
    {
        this.settings = settings;
        this.sampler = sampler;
        this.save = save;
        this.dispatcher = dispatcher;
        this.setTemperatureMetrics = setTemperatureMetrics;
        Words = language;
        Fps = new FpsTrackingViewModel(settings, language, save);
        Disks = new DiskFilterViewModel(settings, language, save);
        var diskCard = new MetricCardViewModel(SystemMetric.Disk, "MetricDisk", this);
        Disks.SelectionChanged += (_, _) => diskCard.ApplyDiskFilter();
        Cards =
        [
            new(SystemMetric.Cpu, "MetricCpu", this),
            new(SystemMetric.Memory, "MetricMemory", this),
            diskCard,
            new(SystemMetric.NvidiaGpu, "MetricNvidia", this),
            new(SystemMetric.Network, "MetricNetwork", this),
            new(SystemMetric.IntegratedGpu, "MetricIntegratedGpu", this)
        ];
    }

    public Language Words { get; }
    public IReadOnlyList<MetricCardViewModel> Cards { get; }
    public FpsTrackingViewModel Fps { get; }
    public DiskFilterViewModel Disks { get; }
    public bool ShowTemperatureDriverLink => Cards.Any(card => card.TemperatureDriverMissing);

    public IReadOnlyList<string> DisplayModes => [Words["MonitoringCards"], Words["MonitoringCharts"]];
    public bool ShowCharts => settings.ShowMonitoringCharts;
    public int DisplayMode
    {
        get => ShowCharts ? 1 : 0;
        set
        {

            if (value is 0 or 1 && ShowCharts != (value == 1))
            {
                settings.ShowMonitoringCharts = value == 1;
                save();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayMode)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowCharts)));
            }
        }
    }

    public bool Enabled
    {
        get => settings.MonitoringEnabled;
        set
        {

            if (value == settings.MonitoringEnabled)
            {

                return;
            }

            settings.MonitoringEnabled = value;
            save();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
            RestartSampling();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetViewing(bool value)
    {

        if (!disposed && viewing != value)
        {
            viewing = value;
            RestartSampling();
        }
    }

    public void RefreshLanguage()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Words)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayModes)));
        Fps.RefreshLanguage();
        Disks.RefreshLanguage();

        foreach (var card in Cards)
        {
            card.Refresh();
        }
    }

    public bool Includes(SystemMetric metric) => (settings.MonitoringMetrics & metric) != 0;

    public void SetMetric(SystemMetric metric, bool enabled)
    {

        if (Includes(metric) == enabled)
        {

            return;
        }

        settings.MonitoringMetrics = enabled ? settings.MonitoringMetrics | metric : settings.MonitoringMetrics & ~metric;
        save();
        RestartSampling();
    }

    private void RestartSampling()
    {
        currentRun?.Cancel();
        currentRun = null;

        foreach (var card in Cards)
        {
            card.Reset(clearHistory: !viewing || !Enabled || !card.Enabled);
        }

        Fps.SetAllowed(viewing && Enabled);
        var active = viewing && Enabled ? settings.MonitoringMetrics : SystemMetric.None;
        setTemperatureMetrics?.Invoke(active);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTemperatureDriverLink)));

        if (active == SystemMetric.None)
        {

            return;
        }

        var run = new CancellationTokenSource();
        currentRun = run;
        currentTask = RunAfterAsync(currentTask, active, run);
    }

    private async Task RunAfterAsync(Task previous, SystemMetric metrics, CancellationTokenSource run)
    {
        try
        {
            await previous;
            run.Token.ThrowIfCancellationRequested();
            await Task.Run(() => sampler.RunAsync(metrics, samples => Publish(samples, run), run.Token), run.Token);
        }
        catch (OperationCanceledException) when (run.IsCancellationRequested)
        {
            // The settings or visible page changed.
        }
        finally
        {

            if (ReferenceEquals(currentRun, run))
            {
                currentRun = null;
            }

            run.Dispose();
        }
    }

    private void Publish(IReadOnlyDictionary<SystemMetric, MetricSample> samples, CancellationTokenSource run)
    {
        _ = dispatcher.InvokeAsync(() =>
        {

            if (!ReferenceEquals(currentRun, run))
            {

                return;
            }

            foreach (var card in Cards)
            {

                if (samples.TryGetValue(card.Metric, out var sample))
                {
                    card.Update(sample);
                }
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTemperatureDriverLink)));
        }, DispatcherPriority.Background);
    }

    public void Dispose()
    {
        SetViewing(false);
        disposed = true;
    }
}
