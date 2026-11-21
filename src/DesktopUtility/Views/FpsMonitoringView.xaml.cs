using System.Windows;
using System.Windows.Controls;
using DesktopUtility.Infrastructure.Shell;
using Microsoft.Win32;

namespace DesktopUtility.Views;

public partial class FpsMonitoringView : UserControl
{
    public FpsMonitoringView() => InitializeComponent();

    private void OpenPresentMonDownload(object sender, RoutedEventArgs args)
    {
        var viewModel = (FpsTrackingViewModel)DataContext;
        Notice.Text = MonitoringReleasePages.OpenPresentMon() ? "" : viewModel.Words["DownloadPageError"];
    }

    private void RefreshWindows(object sender, RoutedEventArgs args)
    {
        var viewModel = (FpsTrackingViewModel)DataContext;
        viewModel.RefreshWindows();
        Notice.Text = "";
    }

    private async void ChooseExecutable(object sender, RoutedEventArgs args)
    {
        var viewModel = (FpsTrackingViewModel)DataContext;
        var picker = new OpenFileDialog
        {
            Title = viewModel.Words["FpsChoosePresentMon"],
            Filter = "PresentMon (*.exe)|PresentMon*.exe"
        };

        if (picker.ShowDialog(Window.GetWindow(this)) == true)
        {
            var button = (Button)sender;
            button.IsEnabled = false;
            try
            {
                Notice.Text = await viewModel.SetExecutableAsync(picker.FileName) ? "" : viewModel.Words["FpsInvalidExecutable"];
            }
            finally
            {
                button.IsEnabled = true;
            }
        }
    }
}
