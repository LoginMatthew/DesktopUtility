using System.Windows.Threading;
using DesktopUtility.Core.Monitoring;
using DesktopUtility.Localization;
using DesktopUtility.Models;
using DesktopUtility.ViewModels;
using Xunit;

namespace DesktopUtility.Wpf.Tests;

public sealed class TemperatureCardTests
{
    [Fact]
    public void MissingDriverIsShownAlongsideRamUsageAndCpuClock()
    {
        using var monitoring = CreateMonitoring();
        var cpu = monitoring.Cards.Single(card => card.Metric == SystemMetric.Cpu);
        var ram = monitoring.Cards.Single(card => card.Metric == SystemMetric.Memory);
        cpu.Update(MetricSample.Available(new CpuReading(20, 3.25, TemperatureStatus: TemperatureStatus.DriverMissing)));
        ram.Update(MetricSample.Available(new MemoryReading(4096, 8192, TemperatureStatus: TemperatureStatus.DriverMissing)));

        Assert.Contains("3.25 GHz", cpu.Details);
        Assert.Contains("PawnIO driver not detected", cpu.Details);
        Assert.Contains("4 KiB used of 8 KiB", ram.Details);
        Assert.Contains("PawnIO driver not detected", ram.Details);
        Assert.True(monitoring.ShowTemperatureDriverLink);
        Assert.Single(ram.Trends);
    }

    [Fact]
    public void RamTemperatureAppearsInDetailsAndItsOwnHistory()
    {
        using var monitoring = CreateMonitoring();
        var ram = monitoring.Cards.Single(card => card.Metric == SystemMetric.Memory);
        ram.Update(MetricSample.Available(new MemoryReading(4096, 8192, 42)));
        ram.Update(MetricSample.Available(new MemoryReading(4096, 8192, 45)));

        Assert.Contains("Hottest RAM module: 45 °C", ram.Details);
        var temperature = Assert.Single(ram.Trends, trend => trend.Label == "Temperature");
        Assert.Equal([42d, 45d], temperature.Samples);
        Assert.False(monitoring.ShowTemperatureDriverLink);
    }

    private static MonitoringViewModel CreateMonitoring()
    {
        var settings = new Settings { MonitoringEnabled = true };
        var sampler = new MonitoringSampler(_ => throw new InvalidOperationException("Unexpected reader"), TimeSpan.FromSeconds(2));

        return new MonitoringViewModel(settings, new Language(), sampler, () => { }, Dispatcher.CurrentDispatcher);
    }
}
