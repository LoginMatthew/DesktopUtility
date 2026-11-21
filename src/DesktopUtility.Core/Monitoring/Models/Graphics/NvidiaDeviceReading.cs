namespace DesktopUtility.Core.Monitoring;

public sealed record NvidiaDeviceReading(string Name, uint? UsagePercent, uint? TemperatureCelsius = null,
    ulong? UsedMemoryBytes = null, ulong? TotalMemoryBytes = null);
