using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DesktopUtility.PresentationControls;

internal static class WindowMaximization
{
    private const int GetMinMaxInfoMessage = 0x0024;
    private const uint NearestMonitor = 2;

    public static void Attach(Window window)
    {
        var source = (HwndSource)PresentationSource.FromVisual(window);
        source.AddHook(UpdateMaximizedBounds);
    }

    private static nint UpdateMaximizedBounds(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {

        if (message != GetMinMaxInfoMessage)
        {

            return 0;
        }

        nint monitor = MonitorFromWindow(window, NearestMonitor);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };

        if (monitor == 0 || !GetMonitorInfo(monitor, ref monitorInfo))
        {

            return 0;
        }

        var bounds = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        bounds.MaxPosition.X = monitorInfo.WorkArea.Left - monitorInfo.Screen.Left;
        bounds.MaxPosition.Y = monitorInfo.WorkArea.Top - monitorInfo.Screen.Top;
        bounds.MaxSize.X = monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left;
        bounds.MaxSize.Y = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top;
        Marshal.StructureToPtr(bounds, lParam, fDeleteOld: false);
        handled = true;

        return 0;
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRectangle Screen;
        public NativeRectangle WorkArea;
        public uint Flags;
    }
}
