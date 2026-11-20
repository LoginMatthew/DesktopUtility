using System.Windows;
using System.Windows.Controls;

namespace DesktopUtility.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel viewModel;

    internal SettingsView(SettingsViewModel viewModel, WallpaperSession session)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
        RotationHost.Content = RotationControls.Build(session, viewModel.Words);
    }

    private void ResetAppearance(object sender, RoutedEventArgs args) => viewModel.ResetAppearance();
}
