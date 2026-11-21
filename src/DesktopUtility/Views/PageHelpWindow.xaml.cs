using System.Windows;

namespace DesktopUtility.Views;

public partial class PageHelpWindow : Window
{
    internal PageHelpWindow(Window owner, PageHelpViewModel viewModel)
    {
        InitializeComponent();
        Owner = owner;
        DataContext = viewModel;
    }
}
