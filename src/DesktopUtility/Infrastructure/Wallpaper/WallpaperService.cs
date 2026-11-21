using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DesktopUtility.Infrastructure.Wallpaper;

internal sealed class WindowsWallpaperService : IWallpaperService, IDisposable
{
    private readonly WallpaperWorker worker = new();
    private const int AccessDenied = unchecked((int)0x80070005);
    private const int InvalidArgument = unchecked((int)0x80070057);
    private const int FileNotFound = unchecked((int)0x80070002);
    private const int PathNotFound = unchecked((int)0x80070003);
    private const int InvalidData = unchecked((int)0x8007000D);
    private const int UnspecifiedFailure = unchecked((int)0x80004005);
    private const int CodecErrorFacility = 0x898;
    private static readonly Guid ClassId = new("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

    public Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync() => worker.RunAsync(() => RunOperation(GetMonitors));

    public Task ApplyAsync(string filePath, string? monitorId) =>
        worker.RunAsync(() => RunOperation(() =>
        {
            Apply(filePath, monitorId);

            return true;
        }));

    public void Dispose() => worker.Dispose();

    private static T RunOperation<T>(Func<T> action)
    {
        try
        {

            return action();
        }
        catch (Exception error) when (error.HResult == AccessDenied)
        {
            throw new WallpaperAccessDeniedException(error);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException)
        {
            Trace.TraceWarning(error.ToString());
            throw new WallpaperOperationException(error);
        }
    }

    private static IReadOnlyList<MonitorTarget> GetMonitors()
    {

        return WithDesktopWallpaper(wallpaper =>
        {
            wallpaper.GetMonitorDevicePathCount(out uint count);
            var monitors = new List<MonitorTarget>();

            for (uint index = 0; index < count; index++)
            {
                wallpaper.GetMonitorDevicePathAt(index, out string id);

                if (wallpaper.GetMonitorRect(id, out var bounds) == 0 && bounds.Right > bounds.Left && bounds.Bottom > bounds.Top)
                {
                    monitors.Add(new MonitorTarget(id, $"{index + 1} · {bounds.Right - bounds.Left} × {bounds.Bottom - bounds.Top} · ({bounds.Left}, {bounds.Top})", checked((int)index + 1)));
                }
            }

            return monitors;
        });
    }

    private static void Apply(string filePath, string? monitorId)
    {

        string fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new WallpaperImageException(new FileNotFoundException("Wallpaper image is unavailable.", fullPath));
        }

        WithDesktopWallpaper(wallpaper =>
        {

            if (monitorId is not null)
            {
                EnsureActiveMonitor(wallpaper, monitorId);
                wallpaper.GetPosition(out var position);

                if (position is WallpaperPosition.Span or WallpaperPosition.Tile)
                {
                    wallpaper.SetPosition(WallpaperPosition.Fill);
                }
            }

            int result = wallpaper.SetWallpaper(monitorId, fullPath);

            if (IsImageRetryCandidate(result))
            {
                try
                {
                    string compatiblePath = WallpaperImageCache.GetCompatibleImage(fullPath);
                    result = wallpaper.SetWallpaper(monitorId, compatiblePath);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or FormatException or COMException)
                {
                    throw new WallpaperImageException(error);
                }
            }

            CheckWallpaperResult(result);

            return true;
        });
    }

    private static void EnsureActiveMonitor(IDesktopWallpaper wallpaper, string monitorId)
    {
        int result = wallpaper.GetMonitorRect(monitorId, out _);

        if (result == 1)
        {
            throw new COMException("The selected display is no longer connected.", InvalidArgument);
        }

        Marshal.ThrowExceptionForHR(result);
    }

    private static bool IsImageRetryCandidate(int result) =>
        result is InvalidArgument or FileNotFound or PathNotFound or InvalidData or UnspecifiedFailure
        || (result < 0 && ((result >> 16) & 0x1FFF) == CodecErrorFacility);

    private static void CheckWallpaperResult(int result)
    {

        if (result == 0)
        {

            return;
        }

        var error = new COMException($"Windows rejected the wallpaper (0x{result:X8}).", result);

        if (result is FileNotFound or PathNotFound or InvalidData || ((result >> 16) & 0x1FFF) == CodecErrorFacility)
        {
            throw new WallpaperImageException(error);
        }

        throw error;
    }

    private static T WithDesktopWallpaper<T>(Func<IDesktopWallpaper, T> action)
    {
        var wallpaper = (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(ClassId)!)!;
        try
        {

            return action(wallpaper);
        }
        finally
        {
            Marshal.ReleaseComObject(wallpaper);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorBounds
    {
        public int Left, Top, Right, Bottom;
    }

    private enum WallpaperPosition
    {
        Tile = 1,
        Fill = 4,
        Span = 5
    }

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        [PreserveSig]
        int SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] out string wallpaper);
        void GetMonitorDevicePathAt(uint monitorIndex, [MarshalAs(UnmanagedType.LPWStr)] out string monitorId);
        void GetMonitorDevicePathCount(out uint count);
        [PreserveSig]
        int GetMonitorRect([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out MonitorBounds displayRect);
        void SetBackgroundColor(uint color);
        void GetBackgroundColor(out uint color);
        void SetPosition(WallpaperPosition position);
        void GetPosition(out WallpaperPosition position);
    }
}
