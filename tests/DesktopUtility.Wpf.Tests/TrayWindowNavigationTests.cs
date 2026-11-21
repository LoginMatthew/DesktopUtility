using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopUtility.Core;
using DesktopUtility.Localization;
using DesktopUtility.Models;
using DesktopUtility.PresentationControls;
using DesktopUtility.Services;
using DesktopUtility.Views;
using Xunit;

namespace DesktopUtility.Wpf.Tests;

public sealed class TrayWindowNavigationTests
{
    [Fact]
    public async Task CustomWindowBoundsApplyTheMinimumSizeToNativeResizing()
    {
        await RunOnStaAsync(() =>
        {
            var window = new Window { MinWidth = 820, MinHeight = 560, ShowActivated = false, ShowInTaskbar = false };
            window.SourceInitialized += (_, _) => WindowMaximization.Attach(window);
            window.Show();
            nint info = Marshal.AllocHGlobal(10 * sizeof(int));

            try
            {
                Marshal.Copy(new int[10], 0, info, 10);
                nint handle = new WindowInteropHelper(window).Handle;
                SendMessage(handle, 0x0024, 0, info);
                var dpi = VisualTreeHelper.GetDpi(window);
                var workArea = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;

                Assert.Equal(Math.Min(workArea.Width, (int)Math.Ceiling(820 * dpi.DpiScaleX)), Marshal.ReadInt32(info, 6 * sizeof(int)));
                Assert.Equal(Math.Min(workArea.Height, (int)Math.Ceiling(560 * dpi.DpiScaleY)), Marshal.ReadInt32(info, 7 * sizeof(int)));
            }
            finally
            {
                Marshal.FreeHGlobal(info);
                window.Close();
            }
        });
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QuickPanelOpensDashboardEvenWhenDeactivationOccursDuringClose(bool minimized)
    {
        await RunOnStaAsync(() =>
        {
            using var session = new WallpaperSession(new Settings(), new UnusedWallpaperService(), TimeProvider.System);
            var words = new Language();
            var dashboard = new Window { ShowActivated = false, ShowInTaskbar = false };
            QuickPanelWindow? quickPanel = null;
            int closedCount = 0;
            quickPanel = new QuickPanelWindow(session, words, () => TrayWindowNavigation.OpenDashboard(dashboard, quickPanel), () => { });
            quickPanel.ShowActivated = false;
            quickPanel.Closing += (_, _) =>
            {
                // Reproduce focus loss during the explicit close from the Open button.
                typeof(Window).GetMethod("OnDeactivated", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(quickPanel, [EventArgs.Empty]);
            };
            quickPanel.Closed += (_, _) => closedCount++;

            try
            {

                if (minimized)
                {
                    dashboard.Show();
                    dashboard.WindowState = WindowState.Minimized;
                }

                quickPanel.Show();
                var buttons = (StackPanel)((Border)quickPanel.Content).Child;
                var open = Assert.Single(buttons.Children.OfType<Button>(), button => Equals(button.Content, words["Open"]));
                open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.True(dashboard.IsVisible);
                Assert.Equal(WindowState.Normal, dashboard.WindowState);
                Assert.Equal(1, closedCount);
            }
            finally
            {

                if (closedCount == 0)
                {
                    quickPanel.Close();
                }

                dashboard.Close();
            }
        });
    }

    private static Task RunOnStaAsync(Action test)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                test();
                completion.SetResult();
            }
            catch (Exception error)
            {
                completion.SetException(error);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }) { IsBackground = true };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
    }

    private sealed class UnusedWallpaperService : IWallpaperService
    {
        public Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync() => throw new NotSupportedException();
        public Task ApplyAsync(string filePath, string? monitorId) => throw new NotSupportedException();
    }
}
