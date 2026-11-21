using System.Runtime.InteropServices;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class MemoryMetricReader(HardwareTemperatureMonitor? temperatures = null) : ISystemMetricReader
{
    public MetricSample Read()
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };

        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhysical == 0)
        {

            return MetricSample.Unavailable;
        }

        ulong used = status.TotalPhysical - Math.Min(status.TotalPhysical, status.AvailablePhysical);
        var temperature = temperatures?.Memory ?? TemperatureReading.Unavailable;

        return MetricSample.Available(new MemoryReading(used, status.TotalPhysical, temperature.Celsius, temperature.Status));
    }

    public void Dispose() { }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
}
