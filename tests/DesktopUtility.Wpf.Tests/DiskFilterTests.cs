using DesktopUtility.Core.Monitoring;
using DesktopUtility.Models;
using DesktopUtility.ViewModels;
using System.Windows.Threading;
using Xunit;

namespace DesktopUtility.Wpf.Tests;

public sealed class DiskFilterTests
{
    [Fact]
    public void DiskGraphsKeepSeparateReadAndWriteHistoryForEachSelectedDrive()
    {
        var settings = new Settings { MonitoringEnabled = true };
        using var monitoring = new MonitoringViewModel(settings, new DesktopUtility.Localization.Language(),
            new MonitoringSampler(_ => throw new InvalidOperationException("Unexpected reader"), TimeSpan.FromSeconds(2)),
            () => { }, Dispatcher.CurrentDispatcher);
        var card = monitoring.Cards.Single(metric => metric.Metric == SystemMetric.Disk);
        var first = new DiskVolumeReading("C:", 800, 1000, "System", 100, 200);
        var second = new DiskVolumeReading("D:", 100, 1000, "Games", 900, 600);
        card.Update(MetricSample.Available(new DiskReading([first, second])));
        card.Update(MetricSample.Available(new DiskReading([first with { ReadBytesPerSecond = 150 }, second])));

        var system = card.Trends.Single(trend => trend.Label == "C: System");
        var games = card.Trends.Single(trend => trend.Label == "D: Games");
        Assert.Equal([100d, 150d], system.Samples);
        Assert.Equal([200d, 200d], system.SecondarySamples);
        Assert.Equal([900d, 900d], games.Samples);
        Assert.Equal([600d, 600d], games.SecondarySamples);

        monitoring.Disks.SetDrive("D:", false);

        Assert.Equal("C: System", Assert.Single(card.Trends).Label);
        Assert.Equal([100d, 150d], card.Trends[0].Samples);
    }

    [Fact]
    public void UncheckingADriveFiltersTotalsAndKeepsNewDrivesOutOfTheManualSelection()
    {
        var settings = new Settings();
        int saves = 0;
        var filter = new DiskFilterViewModel(settings, new DesktopUtility.Localization.Language(), () => saves++);
        var first = new DiskVolumeReading("C:", 800, 1000, ReadBytesPerSecond: 1024);
        var second = new DiskVolumeReading("D:", 100, 1000, ReadBytesPerSecond: 2048, WriteBytesPerSecond: 512);
        var newDrive = new DiskVolumeReading("E:", 400, 1000);
        filter.UpdateDrives([first, second]);
        Assert.All(filter.Drives, drive => Assert.True(drive.Selected));

        filter.Drives[1].Selected = false;
        filter.UpdateDrives([first, second, newDrive]);
        var filtered = new DiskReading(new[] { first, second, newDrive }.Where(volume => filter.Includes(volume.Name)).ToArray());

        Assert.False(filter.AllDrives);
        Assert.Equal(1024d, filtered.ReadBytesPerSecond);
        Assert.Null(filtered.WriteBytesPerSecond);
        Assert.Equal(["C:"], settings.MonitoredDriveRoots);
        Assert.Equal(1, saves);

        filter.AllDrives = true;

        Assert.All(filter.Drives, drive => Assert.True(drive.Selected));
        Assert.Equal(2, saves);
    }
}
