namespace DesktopUtility.Core.Monitoring;

public sealed record DiskReading(IReadOnlyList<DiskVolumeReading> Volumes) : MetricReading
{
    public double? ReadBytesPerSecond => SumRates(Volumes.Select(volume => volume.ReadBytesPerSecond));
    public double? WriteBytesPerSecond => SumRates(Volumes.Select(volume => volume.WriteBytesPerSecond));

    private static double? SumRates(IEnumerable<double?> rates)
    {
        double? total = null;

        foreach (double? rate in rates)
        {

            if (rate is { } value)
            {
                total = (total ?? 0) + value;
            }
        }

        return total;
    }
}
