using System.IO;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class DiskMetricReader : ISystemMetricReader
{
    private readonly WindowsPerformanceQuery query = new();
    private readonly nint readCounter;
    private readonly nint writeCounter;

    public DiskMetricReader()
    {

        readCounter = query.AddCounter(@"\LogicalDisk(*)\Disk Read Bytes/sec");
        writeCounter = query.AddCounter(@"\LogicalDisk(*)\Disk Write Bytes/sec");
        query.Collect();
    }

    public MetricSample Read()
    {
        var volumes = new List<DiskVolumeReading>();
        bool collected = query.Collect();
        var reads = ReadRates(collected, readCounter);
        var writes = ReadRates(collected, writeCounter);

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {

                if (drive.DriveType is (DriveType.Fixed or DriveType.Removable) && drive.IsReady)
                {
                    string instance = drive.Name.TrimEnd('\\');
                    volumes.Add(new DiskVolumeReading(drive.Name, drive.TotalFreeSpace, drive.TotalSize, drive.VolumeLabel,
                        reads.GetValueOrDefault(instance), writes.GetValueOrDefault(instance)));
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // The drive may have disconnected during collection.
            }
        }

        return volumes.Count == 0
            ? MetricSample.Unavailable
            : MetricSample.Available(new DiskReading(volumes));
    }

    private Dictionary<string, double?> ReadRates(bool collected, nint counter)
    {
        var rates = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<PerformanceCounterInstance> instances = collected ? query.ReadInstances(counter) : [];

        foreach (var instance in instances)
        {
            rates.TryAdd(instance.Name, instance.Value);
        }

        return rates;
    }

    public void Dispose() => query.Dispose();
}
