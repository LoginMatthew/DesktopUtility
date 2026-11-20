using System.IO;
using System.Runtime.InteropServices;

namespace DesktopUtility.Infrastructure.Wallpaper;

internal sealed class WindowsWallpaperService : IWallpaperService
{
    private static readonly Guid ClassId = new("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

    public IReadOnlyList<MonitorTarget> GetMonitors()
    {

        return WithDesktopWallpaper(wallpaper =>
        {
            wallpaper.GetMonitorDevicePathCount(out uint count);
            var monitors = new List<MonitorTarget>();

            for (uint index = 0; index < count; index++)
            {
                wallpaper.GetMonitorDevicePathAt(index, out string id);
                wallpaper.GetMonitorRect(id, out var bounds);

                if (bounds.Right > bounds.Left && bounds.Bottom > bounds.Top)
                {
                    monitors.Add(new MonitorTarget(id, $"{index + 1} · {bounds.Right - bounds.Left} × {bounds.Bottom - bounds.Top}"));
                }
            }

            return monitors;
        });
    }

    public void Apply(string filePath, string? monitorId)
    {

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }

        WithDesktopWallpaper(wallpaper =>
        {
            wallpaper.SetWallpaper(monitorId, filePath);

            return true;
        });
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

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] out string wallpaper);
        void GetMonitorDevicePathAt(uint monitorIndex, [MarshalAs(UnmanagedType.LPWStr)] out string monitorId);
        void GetMonitorDevicePathCount(out uint count);
        void GetMonitorRect([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out MonitorBounds displayRect);
    }
}

