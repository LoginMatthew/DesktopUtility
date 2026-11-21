using System.Diagnostics;
using System.Net.NetworkInformation;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class NetworkMetricReader : ISystemMetricReader
{
    private Dictionary<string, InterfaceCounters> previous = new(StringComparer.OrdinalIgnoreCase);
    private long timestamp = Stopwatch.GetTimestamp();

    public MetricSample Read()
    {
        var current = new Dictionary<string, InterfaceCounters>(StringComparer.OrdinalIgnoreCase);

        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {

            if (adapter.OperationalStatus != OperationalStatus.Up
                || adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {

                continue;
            }

            try
            {
                var statistics = adapter.GetIPStatistics();
                current[adapter.Id] = new InterfaceCounters(adapter.Name, statistics.BytesReceived, statistics.BytesSent);
            }
            catch (NetworkInformationException)
            {
                // An adapter can disconnect while its counters are being read.
            }
        }

        var now = Stopwatch.GetTimestamp();
        double seconds = Stopwatch.GetElapsedTime(timestamp, now).TotalSeconds;
        var adapters = new List<NetworkAdapterReading>();

        foreach (var (id, counts) in current)
        {

            if (seconds > 0 && previous.TryGetValue(id, out var last))
            {
                adapters.Add(new NetworkAdapterReading(counts.Name, Math.Max(0, counts.Received - last.Received) / seconds,
                    Math.Max(0, counts.Sent - last.Sent) / seconds));
            }
        }

        previous = current;
        timestamp = now;

        if (current.Count == 0)
        {

            return MetricSample.Unavailable;
        }

        return adapters.Count > 0
            ? MetricSample.Available(new NetworkReading(adapters.Sum(adapter => adapter.DownloadBytesPerSecond),
                adapters.Sum(adapter => adapter.UploadBytesPerSecond),
                adapters.OrderBy(adapter => adapter.Name, StringComparer.CurrentCultureIgnoreCase).ToArray()))
            : MetricSample.Waiting;
    }

    public void Dispose() { }

    private sealed record InterfaceCounters(string Name, long Received, long Sent);
}
