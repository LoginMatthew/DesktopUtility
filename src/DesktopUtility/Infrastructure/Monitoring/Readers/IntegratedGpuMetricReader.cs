using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class IntegratedGpuMetricReader : ISystemMetricReader
{
    private readonly IReadOnlyList<IntegratedAdapter> adapters = DxCoreIntegratedAdapters.Read();
    private readonly WindowsPerformanceQuery query = new();
    private readonly nint counter;

    public IntegratedGpuMetricReader()
    {

        if (adapters.Count > 0)
        {
            counter = query.AddCounter(@"\GPU Engine(*)\Utilization Percentage");
            query.Collect();
        }
    }

    public MetricSample Read()
    {

        if (adapters.Count == 0)
        {

            return MetricSample.Unavailable;
        }

        var counters = query.Collect() ? query.ReadInstances(counter) : [];
        var devices = adapters.Select(adapter => new IntegratedGpuDeviceReading(
            adapter.Name, ReadUsage(adapter.InstanceLuid, counters))).ToArray();

        return MetricSample.Available(new IntegratedGpuReading(devices));
    }

    public void Dispose() => query.Dispose();

    private static double? ReadUsage(string luid, IReadOnlyList<PerformanceCounterInstance> counters)
    {
        var engines = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var counter in counters)
        {
            int position = counter.Name.IndexOf(luid + "_phys_", StringComparison.OrdinalIgnoreCase);

            if (position >= 0)
            {
                string engine = counter.Name[position..].Split('#', 2)[0];
                engines[engine] = engines.GetValueOrDefault(engine) + counter.Value;
            }
        }

        return engines.Count == 0 ? null : Math.Clamp(engines.Values.Max(), 0, 100);
    }
}
