using System.ComponentModel;
using System.Numerics;

namespace DesktopUtility.Core.Monitoring;

public sealed class MonitoringSampler(Func<SystemMetric, ISystemMetricReader> createReader, TimeSpan interval)
{
    private static readonly SystemMetric[] SupportedMetrics = Enum.GetValues<SystemMetric>()
        .Where(metric => BitOperations.IsPow2((uint)metric))
        .ToArray();

    public async Task RunAsync(
        SystemMetric enabledMetrics,
        Action<IReadOnlyDictionary<SystemMetric, MetricSample>> publish,
        CancellationToken cancellationToken)
    {
        var active = SupportedMetrics.Where(metric => (enabledMetrics & metric) != 0).ToArray();

        if (active.Length == 0)
        {

            return;
        }

        var readers = new Dictionary<SystemMetric, ISystemMetricReader>();
        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                var readings = new Dictionary<SystemMetric, MetricSample>();

                foreach (var metric in active)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    readings.Add(metric, Read(metric, readers));
                }

                cancellationToken.ThrowIfCancellationRequested();
                publish(readings);
            } while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
        }
        finally
        {

            foreach (var reader in readers.Values)
            {
                reader.Dispose();
            }
        }
    }

    private MetricSample Read(SystemMetric metric, Dictionary<SystemMetric, ISystemMetricReader> readers)
    {
        try
        {

            if (!readers.TryGetValue(metric, out var reader))
            {
                reader = createReader(metric);
                readers.Add(metric, reader);
            }

            return reader.Read();
        }
        catch (Exception error) when (error is Win32Exception or IOException or UnauthorizedAccessException
            or NotSupportedException or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {

            return MetricSample.Unavailable;
        }
    }
}
