using System.Windows;

namespace DesktopUtility.PresentationControls;

internal static class TrayWindowNavigation
{
    public static void OpenDashboard(Window dashboard, Window? quickPanel)
    {
        quickPanel?.Close();
        dashboard.Show();

        if (dashboard.WindowState == WindowState.Minimized)
        {
            dashboard.WindowState = WindowState.Normal;
        }

        dashboard.Activate();
    }
}
