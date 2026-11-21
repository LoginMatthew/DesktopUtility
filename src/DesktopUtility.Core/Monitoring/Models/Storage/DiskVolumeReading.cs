namespace DesktopUtility.Core.Monitoring;

public sealed record DiskVolumeReading(string Name, long FreeBytes, long TotalBytes, string Label = "",
    double? ReadBytesPerSecond = null, double? WriteBytesPerSecond = null)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? Name : $"{Name} {Label}";
}
