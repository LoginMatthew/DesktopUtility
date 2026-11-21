namespace DesktopUtility.Core.Monitoring;

public static class MonitoringThresholds
{
    public static MonitoringAlertLevel GetFreeSpaceAlert(long freeBytes, long totalBytes)
    {

        if (totalBytes <= 0 || freeBytes < 0 || freeBytes > totalBytes)
        {

            return MonitoringAlertLevel.None;
        }

        return (100m * freeBytes / totalBytes) switch
        {
            <= 15 => MonitoringAlertLevel.Critical,
            <= 25 => MonitoringAlertLevel.Warning,
            _ => MonitoringAlertLevel.None
        };
    }

    public static MonitoringAlertLevel GetLoadAlert(double? usagePercent) => usagePercent switch
    {
        >= 100 and <= double.MaxValue => MonitoringAlertLevel.Critical,
        >= 95 and < 100 => MonitoringAlertLevel.Warning,
        _ => MonitoringAlertLevel.None
    };
}
