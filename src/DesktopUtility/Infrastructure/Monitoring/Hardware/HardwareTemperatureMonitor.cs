using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using DesktopUtility.Core.Monitoring;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.PawnIo;

namespace DesktopUtility.Infrastructure.Monitoring;

internal sealed class HardwareTemperatureMonitor
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(8);
    private const SystemMetric TemperatureMetrics = SystemMetric.Cpu | SystemMetric.Memory;
    private Run? current;
    private Task worker = Task.CompletedTask;

    public TemperatureReading Cpu => Volatile.Read(ref current)?.Cpu ?? TemperatureReading.Unavailable;
    public TemperatureReading Memory => Volatile.Read(ref current)?.Memory ?? TemperatureReading.Unavailable;

    public void SetMetrics(SystemMetric metrics)
    {
        metrics &= TemperatureMetrics;
        var previous = current;

        if ((previous?.Metrics ?? SystemMetric.None) == metrics)
        {

            return;
        }

        previous?.Stop();
        var next = metrics == SystemMetric.None ? null : new Run(metrics);
        Volatile.Write(ref current, next);

        if (next is not null)
        {
            worker = RunAfterAsync(worker, next);
        }
    }

    private static async Task RunAfterAsync(Task previous, Run run)
    {
        try
        {
            await previous.ConfigureAwait(false);
            await Task.Run(() => CollectAsync(run), run.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (run.Token.IsCancellationRequested)
        {
            // The selected metrics or monitoring visibility changed.
        }
        catch (Exception error) when (IsSensorError(error))
        {
            var reading = DescribeError(error);
            run.Cpu = reading;
            run.Memory = reading;
        }
        finally
        {
            run.Complete();
        }
    }

    private static async Task CollectAsync(Run run)
    {
        var computer = new Computer { IsCpuEnabled = run.Metrics.HasFlag(SystemMetric.Cpu) };
        bool driverDetected = PawnIo.IsInstalled;
        bool readMemory = run.Metrics.HasFlag(SystemMetric.Memory) && driverDetected;
        var empty = driverDetected ? TemperatureReading.Unavailable : new TemperatureReading(TemperatureStatus.DriverMissing);

        try
        {
            run.Token.ThrowIfCancellationRequested();
            computer.Open();
            run.Memory = empty;

            if (readMemory)
            {
                try
                {
                    computer.IsMemoryEnabled = true;
                }
                catch (Exception error) when (IsSensorError(error))
                {
                    run.Memory = DescribeError(error);
                    readMemory = false;
                }
            }

            do
            {
                run.Token.ThrowIfCancellationRequested();

                if (computer.IsCpuEnabled)
                {
                    run.Cpu = ReadGroup(computer, HardwareType.Cpu, empty);
                }

                if (readMemory)
                {
                    run.Memory = ReadGroup(computer, HardwareType.Memory, empty);
                }

                await Task.Delay(RefreshInterval, run.Token).ConfigureAwait(false);
            }
            while (!run.Token.IsCancellationRequested);
        }
        finally
        {
            computer.Close();
        }
    }

    private static TemperatureReading ReadGroup(Computer computer, HardwareType type, TemperatureReading empty)
    {
        try
        {
            double? hottest = null;
            bool dimmOnly = type == HardwareType.Memory;

            foreach (var hardware in computer.Hardware.Where(device => device.HardwareType == type))
            {

                if (dimmOnly && !hardware.Identifier.ToString().StartsWith("/memory/dimm/", StringComparison.Ordinal))
                {
                    continue;
                }

                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    // DIMM temperature limits also use SensorType.Temperature.

                    if (sensor.SensorType == SensorType.Temperature && (!dimmOnly || sensor.Index == 0)
                        && sensor.Value is float value && value is > 0 and <= 150)
                    {
                        hottest = Math.Max(hottest ?? value, value);
                    }
                }
            }

            return hottest is { } measured ? new TemperatureReading(TemperatureStatus.Available, measured) : empty;
        }
        catch (Exception error) when (IsSensorError(error))
        {

            return DescribeError(error);
        }
    }

    private static bool IsSensorError(Exception error) =>
        error is IOException or UnauthorizedAccessException or SecurityException or Win32Exception or COMException
            or InvalidOperationException or NotSupportedException or TypeInitializationException or DllNotFoundException
            or EntryPointNotFoundException or BadImageFormatException;

    private static TemperatureReading DescribeError(Exception error)
    {
        Trace.TraceWarning(error.ToString());

        return error is UnauthorizedAccessException or SecurityException or Win32Exception { NativeErrorCode: 5 }
            ? new TemperatureReading(TemperatureStatus.AccessDenied)
            : TemperatureReading.Unavailable;
    }

    private sealed class Run(SystemMetric metrics)
    {
        private readonly CancellationTokenSource cancellation = new();
        private readonly object gate = new();
        private bool complete;
        private TemperatureReading cpu = new(TemperatureStatus.Waiting);
        private TemperatureReading memory = new(TemperatureStatus.Waiting);

        public SystemMetric Metrics => metrics;
        public CancellationToken Token => cancellation.Token;
        public TemperatureReading Cpu { get => Volatile.Read(ref cpu); set => Volatile.Write(ref cpu, value); }
        public TemperatureReading Memory { get => Volatile.Read(ref memory); set => Volatile.Write(ref memory, value); }

        public void Stop()
        {
            lock (gate)
            {

                if (!complete)
                {
                    cancellation.Cancel();
                }
            }
        }

        public void Complete()
        {
            lock (gate)
            {
                complete = true;
                cancellation.Dispose();
            }
        }
    }
}
