using DesktopUtility.Core.Monitoring;
using Xunit;

namespace DesktopUtility.Core.Tests;

public sealed class MonitoringSamplerTests
{
    [Fact]
    public async Task DisabledMonitoringNeverCreatesAReader()
    {
        var sampler = new MonitoringSampler(_ => throw new InvalidOperationException("A reader was created."), TimeSpan.FromSeconds(2));

        await sampler.RunAsync(SystemMetric.None, _ => throw new InvalidOperationException("A sample was emitted."),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SamplingCreatesOnlySelectedReadersAndDisposesThemOnCancellation()
    {
        var records = new Dictionary<SystemMetric, RecordingReader>();
        var sampler = new MonitoringSampler(metric =>
        {
            var reader = new RecordingReader(() => MetricSample.Available(new CpuReading(42)));
            records.Add(metric, reader);

            return reader;
        }, TimeSpan.FromSeconds(2));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sampler.RunAsync(SystemMetric.Cpu | SystemMetric.Disk, samples =>
            {
                Assert.Equal([SystemMetric.Cpu, SystemMetric.Disk], samples.Keys);
                stop.Cancel();
            }, stop.Token));

        Assert.Equal([SystemMetric.Cpu, SystemMetric.Disk], records.Keys);
        Assert.All(records.Values, reader =>
        {
            Assert.Equal(1, reader.ReadCount);
            Assert.True(reader.Disposed);
        });
    }

    [Fact]
    public async Task AFailedReaderDoesNotStopOtherMetrics()
    {
        var sampler = new MonitoringSampler(metric => metric == SystemMetric.NvidiaGpu
            ? new RecordingReader(() => throw new UnauthorizedAccessException())
            : new RecordingReader(() => MetricSample.Available(new MemoryReading(1024, 2048))), TimeSpan.FromSeconds(2));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sampler.RunAsync(SystemMetric.Memory | SystemMetric.NvidiaGpu, samples =>
            {
                Assert.IsType<MemoryReading>(samples[SystemMetric.Memory].Reading);
                Assert.Equal(MetricStatus.Unavailable, samples[SystemMetric.NvidiaGpu].Status);
                stop.Cancel();
            }, stop.Token));
    }

    private sealed class RecordingReader(Func<MetricSample> read) : ISystemMetricReader
    {
        public int ReadCount { get; private set; }
        public bool Disposed { get; private set; }

        public MetricSample Read()
        {
            ReadCount++;

            return read();
        }

        public void Dispose() => Disposed = true;
    }
}
