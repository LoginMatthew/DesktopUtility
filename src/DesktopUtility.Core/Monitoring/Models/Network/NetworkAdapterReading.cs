namespace DesktopUtility.Core.Monitoring;

public sealed record NetworkAdapterReading(string Name, double DownloadBytesPerSecond, double UploadBytesPerSecond);
