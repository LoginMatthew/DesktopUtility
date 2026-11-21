using DesktopUtility.Core.Monitoring;
using Xunit;

namespace DesktopUtility.Core.Tests;

public sealed class MonitoringThresholdTests
{
    [Theory]
    [InlineData(0, 100, MonitoringAlertLevel.Critical)]
    [InlineData(15, 100, MonitoringAlertLevel.Critical)]
    [InlineData(151, 1000, MonitoringAlertLevel.Warning)]
    [InlineData(25, 100, MonitoringAlertLevel.Warning)]
    [InlineData(251, 1000, MonitoringAlertLevel.None)]
    [InlineData(0, 0, MonitoringAlertLevel.None)]
    [InlineData(-1, 100, MonitoringAlertLevel.None)]
    [InlineData(101, 100, MonitoringAlertLevel.None)]
    public void FreeSpaceAlertsUseEachVolumesCapacity(long free, long total, MonitoringAlertLevel expected) =>
        Assert.Equal(expected, MonitoringThresholds.GetFreeSpaceAlert(free, total));

    [Theory]
    [InlineData(null, MonitoringAlertLevel.None)]
    [InlineData(94.99, MonitoringAlertLevel.None)]
    [InlineData(95d, MonitoringAlertLevel.Warning)]
    [InlineData(99.99, MonitoringAlertLevel.Warning)]
    [InlineData(100d, MonitoringAlertLevel.Critical)]
    [InlineData(double.NaN, MonitoringAlertLevel.None)]
    public void LoadAlertsUseTheMeasuredValue(double? percentage, MonitoringAlertLevel expected) =>
        Assert.Equal(expected, MonitoringThresholds.GetLoadAlert(percentage));
}
