using System.ComponentModel;
using System.Diagnostics;

namespace DesktopUtility.Infrastructure.Shell;

internal static class MonitoringReleasePages
{
    private const string PresentMon = "https://github.com/GameTechDev/PresentMon/releases/tag/v2.6.0";
    private const string PawnIo = "https://pawnio.eu/";

    public static bool OpenPresentMon() => TryOpen(PresentMon);
    public static bool OpenPawnIo() => TryOpen(PawnIo);

    private static bool TryOpen(string url)
    {
        try
        {
            using var browser = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            return true;
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            Trace.TraceWarning(error.ToString());

            return false;
        }
    }
}
