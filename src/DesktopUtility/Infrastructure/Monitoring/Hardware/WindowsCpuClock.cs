using System.Runtime.InteropServices;

namespace DesktopUtility.Infrastructure.Monitoring;

internal static class WindowsCpuClock
{
    private const int ProcessorInformation = 11;
    private const ushort AllProcessorGroups = 0xffff;

    public static double? ReadGhz()
    {
        uint count = GetActiveProcessorCount(AllProcessorGroups);

        if (count == 0)
        {

            return null;
        }

        var processors = new ProcessorPowerInformation[count];
        uint size = checked((uint)Marshal.SizeOf<ProcessorPowerInformation>() * count);

        if (CallNtPowerInformation(ProcessorInformation, 0, 0, processors, size) != 0)
        {

            return null;
        }

        var speeds = processors.Where(processor => processor.CurrentMhz > 0).Select(processor => (double)processor.CurrentMhz);

        double mean = speeds.DefaultIfEmpty(0).Average();

        return mean > 0 ? mean / 1000 : null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessorPowerInformation
    {
        public uint Number;
        public uint MaxMhz;
        public uint CurrentMhz;
        public uint MhzLimit;
        public uint MaxIdleState;
        public uint CurrentIdleState;
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern uint GetActiveProcessorCount(ushort groupNumber);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint CallNtPowerInformation(int informationLevel, nint inputBuffer, uint inputSize,
        [Out] ProcessorPowerInformation[] outputBuffer, uint outputSize);
}
