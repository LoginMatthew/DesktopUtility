using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class NvidiaGpuMetricReader : ISystemMetricReader
{
    private nint library;
    private NvmlCall? shutdown;
    private readonly DeviceCount? getCount;
    private readonly DeviceAt? getDevice;
    private readonly DeviceName? getName;
    private readonly DeviceUtilization? getUtilization;
    private readonly DeviceTemperature? getTemperature;
    private readonly DeviceMemory? getMemory;

    public NvidiaGpuMetricReader()
    {
        string systemLibrary = Path.Combine(Environment.SystemDirectory, "nvml.dll");
        string legacyLibrary = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "NVIDIA Corporation", "NVSMI", "nvml.dll");

        if (!NativeLibrary.TryLoad(systemLibrary, out library) && !NativeLibrary.TryLoad(legacyLibrary, out library))
        {

            return;
        }

        try
        {
            var initialize = GetFunction<NvmlCall>("nvmlInit_v2");
            var release = GetFunction<NvmlCall>("nvmlShutdown");
            getCount = GetFunction<DeviceCount>("nvmlDeviceGetCount_v2");
            getDevice = GetFunction<DeviceAt>("nvmlDeviceGetHandleByIndex_v2");
            getName = GetFunction<DeviceName>("nvmlDeviceGetName");
            getUtilization = GetFunction<DeviceUtilization>("nvmlDeviceGetUtilizationRates");
            getTemperature = TryGetFunction<DeviceTemperature>("nvmlDeviceGetTemperature");
            getMemory = TryGetFunction<DeviceMemory>("nvmlDeviceGetMemoryInfo");

            if (initialize() == 0)
            {
                shutdown = release;
            }
            else
            {
                Dispose();
            }
        }
        catch (EntryPointNotFoundException)
        {
            Dispose();
        }
    }

    public MetricSample Read()
    {

        if (shutdown is null || getCount!(out uint count) != 0 || count == 0)
        {

            return MetricSample.Unavailable;
        }

        var devices = new List<NvidiaDeviceReading>();

        for (uint index = 0; index < count; index++)
        {

            if (getDevice!(index, out nint device) != 0)
            {
                continue;
            }

            byte[] nameBuffer = new byte[128];
            string name = getName!(device, nameBuffer, (uint)nameBuffer.Length) == 0
                ? Encoding.UTF8.GetString(nameBuffer).TrimEnd('\0')
                : $"NVIDIA {index + 1}";
            uint? usage = getUtilization!(device, out var utilization) == 0 ? Math.Min(100u, utilization.Gpu) : null;
            uint? temperature = getTemperature is not null && getTemperature(device, 0, out uint value) == 0
                ? value : null;
            MemoryInfo? memory = getMemory is not null && getMemory(device, out var info) == 0 ? info : null;
            devices.Add(new NvidiaDeviceReading(name, usage, temperature, memory?.Used, memory?.Total));
        }

        return devices.Count == 0 ? MetricSample.Unavailable : MetricSample.Available(new NvidiaReading(devices));
    }

    public void Dispose()
    {

        if (library != 0)
        {
            shutdown?.Invoke();
            shutdown = null;
            NativeLibrary.Free(library);
            library = 0;
        }
    }

    private T GetFunction<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    private T? TryGetFunction<T>(string name) where T : Delegate =>
        NativeLibrary.TryGetExport(library, name, out nint pointer) ? Marshal.GetDelegateForFunctionPointer<T>(pointer) : null;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NvmlCall();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeviceCount(out uint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeviceAt(uint index, out nint device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeviceName(nint device, [Out] byte[] name, uint length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeviceUtilization(nint device, out Utilization utilization);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeviceTemperature(nint device, uint sensor, out uint temperature);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DeviceMemory(nint device, out MemoryInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct Utilization
    {
        public uint Gpu;
        public uint Memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryInfo
    {
        public ulong Total;
        public ulong Free;
        public ulong Used;
    }
}
