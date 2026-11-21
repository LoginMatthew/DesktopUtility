namespace DesktopUtility.Core.Monitoring;

public sealed record NetworkReading(double DownloadBytesPerSecond, double UploadBytesPerSecond,
    IReadOnlyList<NetworkAdapterReading>? Adapters = null) : MetricReading;
