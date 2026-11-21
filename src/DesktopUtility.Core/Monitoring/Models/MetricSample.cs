namespace DesktopUtility.Core.Monitoring;

public readonly record struct MetricSample(MetricStatus Status, MetricReading? Reading = null)
{
    public static MetricSample Available(MetricReading reading) => new(MetricStatus.Available, reading);
    public static MetricSample Waiting => new(MetricStatus.Waiting);
    public static MetricSample Unavailable => new(MetricStatus.Unavailable);
}
