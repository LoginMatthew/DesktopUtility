using System.Runtime.InteropServices;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class WindowsPerformanceQuery : IDisposable
{
    private const uint DoubleFormat = 0x00000200;
    private const uint MoreData = 0x800007D2;
    private nint query;

    public WindowsPerformanceQuery()
    {

        if (PdhOpenQuery(null, 0, out query) != 0)
        {
            query = 0;
        }
    }

    public nint AddCounter(string path) =>
        query != 0 && PdhAddEnglishCounter(query, path, 0, out nint counter) == 0 ? counter : 0;

    public bool Collect() => query != 0 && PdhCollectQueryData(query) == 0;

    public IReadOnlyList<PerformanceCounterInstance> ReadInstances(nint counter)
    {
        uint size = 0;

        if (counter == 0 || PdhGetFormattedCounterArray(counter, DoubleFormat, ref size, out _, 0) != MoreData || size == 0)
        {

            return [];
        }

        nint buffer = Marshal.AllocHGlobal(checked((int)size));

        try
        {

            if (PdhGetFormattedCounterArray(counter, DoubleFormat, ref size, out uint count, buffer) != 0)
            {

                return [];
            }

            int stride = Marshal.SizeOf<FormattedCounterItem>();
            var instances = new List<PerformanceCounterInstance>();

            for (int index = 0; index < count; index++)
            {
                var item = Marshal.PtrToStructure<FormattedCounterItem>(buffer + index * stride);
                string? name = Marshal.PtrToStringUni(item.Name);

                if (name is not null && IsValid(item.Value))
                {
                    instances.Add(new PerformanceCounterInstance(name, item.Value.Value));
                }
            }

            return instances;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Dispose()
    {

        if (query != 0)
        {
            PdhCloseQuery(query);
            query = 0;
        }
    }

    private static bool IsValid(FormattedCounterValue value) => value.Status <= 1 && double.IsFinite(value.Value) && value.Value >= 0;

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct FormattedCounterValue
    {
        [FieldOffset(0)] public uint Status;
        [FieldOffset(8)] public double Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FormattedCounterItem
    {
        public nint Name;
        public FormattedCounterValue Value;
    }

    [DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint PdhOpenQuery(string? dataSource, nuint userData, out nint query);

    [DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint PdhAddEnglishCounter(nint query, string counterPath, nuint userData, out nint counter);

    [DllImport("pdh.dll", ExactSpelling = true)]
    private static extern uint PdhCollectQueryData(nint query);

    [DllImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterArrayW", ExactSpelling = true)]
    private static extern uint PdhGetFormattedCounterArray(nint counter, uint format, ref uint size, out uint count, nint buffer);

    [DllImport("pdh.dll", ExactSpelling = true)]
    private static extern uint PdhCloseQuery(nint query);
}
