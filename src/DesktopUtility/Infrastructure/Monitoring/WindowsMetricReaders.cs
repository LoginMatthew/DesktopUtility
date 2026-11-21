using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring;

internal static class WindowsMetricReaders
{
    public static ISystemMetricReader Create(SystemMetric metric, HardwareTemperatureMonitor? temperatures = null) => metric switch
    {
        SystemMetric.Cpu => new CpuMetricReader(temperatures),
        SystemMetric.Memory => new MemoryMetricReader(temperatures),
        SystemMetric.Disk => new DiskMetricReader(),
        SystemMetric.NvidiaGpu => new NvidiaGpuMetricReader(),
        SystemMetric.Network => new NetworkMetricReader(),
        SystemMetric.IntegratedGpu => new IntegratedGpuMetricReader(),
        _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };
}
