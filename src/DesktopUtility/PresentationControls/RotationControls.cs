using System.Windows;
using System.Windows.Controls;
using static DesktopUtility.PresentationControls.Controls;

namespace DesktopUtility.PresentationControls;

internal static class RotationControls
{
    public static StackPanel Build(WallpaperSession session, Language words)
    {
        var viewModel = new RotationControlsViewModel(session);
        var panel = new StackPanel();
        var enabled = CheckBox(words["Rotate"], viewModel.Enabled, value => viewModel.Enabled = value);
        panel.Children.Add(enabled);
        var row = new WrapPanel();
        var interval = new TextBox { Text = viewModel.IntervalMinutes.ToString(), Width = 60, Margin = new Thickness(0, 0, 9, 6), VerticalContentAlignment = VerticalAlignment.Center };
        System.Windows.Automation.AutomationProperties.SetName(interval, words["Minutes"]);

        foreach (int minutes in new[] { 5, 15, 30, 60 })
        {
            row.Children.Add(Button(minutes.ToString(), () => viewModel.UpdateInterval(minutes.ToString())));
        }
        panel.Children.Add(Label(words["Minutes"]));
        panel.Children.Add(row);
        var custom = new WrapPanel();
        custom.Children.Add(interval);
        custom.Children.Add(Button(words["Save"], () => viewModel.UpdateInterval(interval.Text)));
        panel.Children.Add(custom);
        void Synchronize()
        {
            enabled.IsChecked = viewModel.Enabled;
            interval.Text = viewModel.IntervalMinutes.ToString();
        }

        panel.Loaded += (_, _) =>
        {
            Synchronize();
            viewModel.Changed += Synchronize;
        };

        panel.Unloaded += (_, _) => viewModel.Changed -= Synchronize;

        return panel;
    }
}

