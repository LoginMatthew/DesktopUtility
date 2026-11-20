using System.Windows.Controls;

namespace DesktopUtility.Views;

public partial class AboutView : UserControl
{
    internal AboutView(AboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
