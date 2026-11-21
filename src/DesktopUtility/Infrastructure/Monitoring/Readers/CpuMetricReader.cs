using System.Runtime.InteropServices;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class CpuMetricReader(HardwareTemperatureMonitor? temperatures = null) : ISystemMetricReader
{
    private CpuTime? previous;

    public MetricSample Read()
    {

        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            previous = null;

            return MetricSample.Unavailable;
        }

        var current = new CpuTime(idle.Ticks, kernel.Ticks + user.Ticks);
        var old = previous;
        previous = current;

        if (old is not { } baseline || current.Total <= baseline.Total || current.Idle < baseline.Idle)
        {

            return MetricSample.Waiting;
        }

        double idleShare = (current.Idle - baseline.Idle) / (double)(current.Total - baseline.Total);
        var temperature = temperatures?.Cpu ?? TemperatureReading.Unavailable;

        return MetricSample.Available(new CpuReading(Math.Clamp(100 * (1 - idleShare), 0, 100),
            WindowsCpuClock.ReadGhz(), temperature.Celsius, temperature.Status));
    }

    public void Dispose() { }

    private readonly record struct CpuTime(ulong Idle, ulong Total);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint Low;
        public uint High;
        public readonly ulong Ticks => ((ulong)High << 32) | Low;
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
}
