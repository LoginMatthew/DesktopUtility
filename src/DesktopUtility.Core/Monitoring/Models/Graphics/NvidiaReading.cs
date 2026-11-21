namespace DesktopUtility.Core.Monitoring;

public sealed record NvidiaReading(IReadOnlyList<NvidiaDeviceReading> Devices) : MetricReading;
