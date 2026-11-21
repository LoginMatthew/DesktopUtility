namespace DesktopUtility.Core.Monitoring;

public sealed record IntegratedGpuReading(IReadOnlyList<IntegratedGpuDeviceReading> Devices) : MetricReading;
