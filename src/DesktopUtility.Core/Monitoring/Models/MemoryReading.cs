namespace DesktopUtility.Core.Monitoring;

public sealed record MemoryReading(ulong UsedBytes, ulong TotalBytes, double? TemperatureCelsius = null,
    TemperatureStatus TemperatureStatus = TemperatureStatus.Unavailable) : MetricReading;
