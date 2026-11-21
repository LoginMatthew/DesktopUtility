using System.Runtime.InteropServices;
using System.Text;
using Forms = System.Windows.Forms;

namespace DesktopUtility.Infrastructure.Monitoring.Fps;

internal static class DesktopWindowCatalog
{
    private const int CloakedAttribute = 14;

    public static IReadOnlyList<MonitorWindows> Read()
    {
        var screens = Forms.Screen.AllScreens;
        var windows = screens.ToDictionary(screen => screen.DeviceName, _ => new List<WindowCaptureTarget>(), StringComparer.OrdinalIgnoreCase);
        EnumWindows((handle, _) =>
        {

            if (!IsWindowVisible(handle) || IsCloaked(handle))
            {

                return true;
            }

            GetWindowThreadProcessId(handle, out uint pid);

            if (pid == 0 || pid == Environment.ProcessId || pid > int.MaxValue)
            {

                return true;
            }

            var buffer = new StringBuilder(512);

            if (GetWindowText(handle, buffer, buffer.Capacity) > 0)
            {
                string monitorId = Forms.Screen.FromHandle(handle).DeviceName;
                string title = buffer.ToString().Trim();

                if (title.Length > 0 && windows.TryGetValue(monitorId, out var list))
                {
                    list.Add(new WindowCaptureTarget(handle, (int)pid, title));
                }
            }

            return true;
        }, 0);

        return screens.Select((screen, index) => new MonitorWindows(screen.DeviceName,
            $"{index + 1} · {screen.Bounds.Width} × {screen.Bounds.Height} · ({screen.Bounds.X}, {screen.Bounds.Y})",
            windows[screen.DeviceName].OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase).ToArray())).ToArray();
    }

    public static bool IsCurrent(WindowCaptureTarget window) =>
        GetWindowThreadProcessId(window.Handle, out uint pid) != 0 && pid == window.ProcessId;

    private static bool IsCloaked(nint window) =>
        DwmGetWindowAttribute(window, CloakedAttribute, out uint cloaked, sizeof(uint)) == 0 && cloaked != 0;

    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool EnumWindow(nint window, nint parameter);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindow callback, nint parameter);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetWindowText(nint window, StringBuilder text, int capacity);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out uint value, int size);
}
