using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Infrastructure.Monitoring.Fps;

internal static class PresentMonCapture
{
    private const string SupportedHash = "B2A706BC6AD475749E3B7E3409263AA1E6906D45BDCF993F6DBC0F660188F1AF";
    public static bool IsConsolePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path)
        && string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase)
        && Path.GetFileName(path).StartsWith("PresentMon", StringComparison.OrdinalIgnoreCase)
        && File.Exists(path);

    public static bool IsSupported(string path)
    {
        try
        {

            if (!IsConsolePath(path))
            {

                return false;
            }

            using var binary = File.OpenRead(path);

            return MatchesRelease(binary);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {

            return false;
        }
    }

    private static bool MatchesRelease(FileStream binary) =>
        binary.Length <= 2_000_000 && Convert.ToHexString(SHA256.HashData(binary)) == SupportedHash;

    public static async Task CaptureAsync(string executable, int processId, Action<double?> publish, CancellationToken cancellationToken)
    {
        using var binary = File.OpenRead(executable);

        if (!MatchesRelease(binary))
        {
            throw new PresentMonVersionException();
        }

        using var target = Process.GetProcessById(processId);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        string session = $"DesktopUtility-{Environment.ProcessId}-{Guid.NewGuid():N}";

        foreach (string argument in new[] { "--process_id", processId.ToString(CultureInfo.InvariantCulture),
            "--output_stdout", "--no_console_stats", "--no_track_input", "--no_track_gpu",
            "--terminate_on_proc_exit", "--session_name", session })
        {
            start.ArgumentList.Add(argument);
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var process = Process.Start(start) ?? throw new InvalidOperationException("PresentMon did not start.");
        var rates = new PresentMonFrameRates(processId);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            var output = ReadFramesAsync(process.StandardOutput, rates, cancellationToken);
            var errors = process.StandardError.ReadToEndAsync(cancellationToken);

            while (!output.IsCompleted)
            {
                var tick = timer.WaitForNextTickAsync(cancellationToken).AsTask();

                if (await Task.WhenAny(tick, output).ConfigureAwait(false) == output)
                {
                    break;
                }

                await tick.ConfigureAwait(false);

                if (target.HasExited)
                {

                    return;
                }

                publish(rates.TakeFramesPerSecond());
            }

            await output.ConfigureAwait(false);
            string diagnostics = await errors.ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                Trace.TraceWarning($"PresentMon exit {process.ExitCode}: {diagnostics}");

                if (diagnostics.Contains("access denied", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("PresentMon could not open its ETW session.");
                }

                throw new InvalidOperationException($"PresentMon ended with exit code {process.ExitCode}.");
            }
        }
        finally
        {
            await StopOwnedCaptureAsync(process, executable, session).ConfigureAwait(false);
        }
    }

    private static async Task StopOwnedCaptureAsync(Process capture, string executable, string session)
    {

        if (capture.HasExited)
        {

            return;
        }

        try
        {
            var stop = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
            stop.ArgumentList.Add("--session_name");
            stop.ArgumentList.Add(session);
            stop.ArgumentList.Add("--terminate_existing_session");
            using var terminator = Process.Start(stop);

            if (terminator is not null)
            {
                try
                {
                    await WaitForExitAsync(terminator).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    terminator.Kill();
                    await WaitForExitAsync(terminator).ConfigureAwait(false);
                }
            }
        }
        catch (Exception error) when (error is InvalidOperationException or Win32Exception or TimeoutException)
        {
            // The executable or session may no longer be available.
        }

        try
        {
            await WaitForExitAsync(capture).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            try
            {
                capture.Kill();
                await WaitForExitAsync(capture).ConfigureAwait(false);
            }
            catch (Exception error) when (error is InvalidOperationException or Win32Exception or TimeoutException)
            {
                Trace.TraceWarning(error.ToString());
            }
        }
    }

    private static Task WaitForExitAsync(Process process) =>
        process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));

    private static async Task ReadFramesAsync(StreamReader output, PresentMonFrameRates rates, CancellationToken cancellationToken)
    {

        while (await output.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            rates.AddLine(line);
        }
    }
}
