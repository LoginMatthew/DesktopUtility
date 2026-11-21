namespace DesktopUtility.Core.Monitoring;

public sealed record CpuReading(double UsagePercent, double? ClockGhz = null, double? TemperatureCelsius = null,
    TemperatureStatus TemperatureStatus = TemperatureStatus.Unavailable) : MetricReading;
