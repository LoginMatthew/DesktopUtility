using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace DesktopUtility.Views;

internal sealed class QuickPanelWindow : Window
{
    private bool closing;

    public QuickPanelWindow(WallpaperSession state, Language words, Action openDashboard, Action exitApplication)
    {

        var panel = new StackPanel { Margin = new Thickness(17) };
        var window = this;
        Title = "Desktop Utility";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Width = 350;
        SizeToContent = SizeToContent.Height;
        Background = Brushes.Transparent;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 14;
        SetResourceReference(ForegroundProperty, "AppText");
        var chrome = new Border { CornerRadius = new CornerRadius(13), Child = panel, BorderThickness = new Thickness(1) };
        chrome.SetResourceReference(Border.BackgroundProperty, "WindowSurface");
        chrome.SetResourceReference(Border.BorderBrushProperty, "ChromeStroke");
        Content = chrome;
        panel.Children.Add(new TextBlock { Text = "Desktop Utility", FontSize = 20 });
        var status = Controls.Label("");
        status.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
        var notice = Controls.Label("");
        var statusModel = new RotationStatusViewModel(state, words);
        void UpdateStatus()
        {
            status.Text = statusModel.Status;
            notice.Text = statusModel.Notice;
            notice.Visibility = string.IsNullOrEmpty(notice.Text) ? Visibility.Collapsed : Visibility.Visible;
        }

        panel.Children.Add(status);
        panel.Children.Add(RotationControls.Build(state, words));
        panel.Children.Add(Controls.Button(words["ApplyFavorite"], state.ApplyFavorite));
        panel.Children.Add(Controls.Button(words["Open"], openDashboard));
        panel.Children.Add(Controls.Button(words["Exit"], exitApplication));
        panel.Children.Add(notice);
        state.StateUpdated += UpdateStatus;
        UpdateStatus();

        window.KeyDown += (_, args) =>
        {

            if (args.Key == System.Windows.Input.Key.Escape)
            {
                window.Close();
            }
        };

        window.Closed += (_, _) =>
        {
            state.StateUpdated -= UpdateStatus;
            statusModel.Dispose();

        };

        var cursor = Forms.Cursor.Position;
        var workArea = Forms.Screen.FromPoint(cursor).WorkingArea;
        window.SourceInitialized += (_, _) =>
        {
            var source = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(window);
            var transform = source.CompositionTarget.TransformFromDevice;
            var topLeft = transform.Transform(new Point(workArea.Left, workArea.Top));
            var bottomRight = transform.Transform(new Point(workArea.Right, workArea.Bottom));
            var anchor = transform.Transform(new Point(cursor.X, cursor.Y));
            window.Left = Math.Max(topLeft.X, Math.Min(anchor.X - window.Width, bottomRight.X - window.Width));
            window.Top = topLeft.Y;
            window.ContentRendered += (_, _) =>
            {
                window.Top = Math.Max(topLeft.Y, Math.Min(anchor.Y - window.ActualHeight, bottomRight.Y - window.ActualHeight));
            };
        };

    }

    protected override void OnClosing(CancelEventArgs args)
    {
        closing = true;
        base.OnClosing(args);

        if (args.Cancel)
        {
            closing = false;
        }
    }

    protected override void OnDeactivated(EventArgs args)
    {
        base.OnDeactivated(args);

        if (!closing && IsVisible)
        {
            Close();
        }
    }
}
