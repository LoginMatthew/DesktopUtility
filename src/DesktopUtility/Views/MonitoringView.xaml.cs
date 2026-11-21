using System.Windows;
using System.Windows.Controls;
using DesktopUtility.Infrastructure.Shell;

namespace DesktopUtility.Views;

public partial class MonitoringView : UserControl
{
    internal MonitoringView(MonitoringViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OpenTemperatureDriverPage(object sender, RoutedEventArgs args)
    {
        var viewModel = (MonitoringViewModel)DataContext;
        TemperatureDriverNotice.Text = MonitoringReleasePages.OpenPawnIo() ? "" : viewModel.Words["DownloadPageError"];
    }
}
