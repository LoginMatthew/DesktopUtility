using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using static DesktopUtility.PresentationControls.Controls;

namespace DesktopUtility.Views;

internal sealed class MonitorPoolControls : StackPanel
{
    private readonly GalleryViewModel viewModel;
    private readonly Language words;
    private readonly Action render;
    private readonly Action poolChanged;
    private readonly ComboBox monitors = new() { MinWidth = 155 };
    private readonly ComboBox filters = new() { MinWidth = 155 };
    private readonly Button add;
    private readonly Button remove;
    private readonly Button applyRandom;
    private readonly TextBlock count;
    private IReadOnlyList<string> selectedImages = [];
    private bool loadingMonitors;
    private bool targetAvailable;

    public MonitorPoolControls(GalleryViewModel viewModel, Language words, Action render, Action poolChanged)
    {
        this.viewModel = viewModel;
        this.words = words;
        this.render = render;
        this.poolChanged = poolChanged;
        Margin = new Thickness(0, 3, 0, 5);

        var choices = new WrapPanel();
        AutomationProperties.SetName(monitors, words["TargetMonitor"]);
        monitors.IsEnabled = false;
        choices.Children.Add(Label(words["TargetMonitor"]));
        choices.Children.Add(monitors);
        AutomationProperties.SetName(filters, words["MonitorPoolFilter"]);
        choices.Children.Add(Label(words["MonitorPoolFilter"]));
        choices.Children.Add(filters);
        choices.Children.Add(CheckBox(words["Separate"], viewModel.SeparateMonitors, enabled => viewModel.SeparateMonitors = enabled));
        Children.Add(choices);

        var actions = new WrapPanel();
        add = Button(words["AddToMonitorPool"], () => UpdatePool(include: true));
        remove = Button(words["RemoveFromMonitorPool"], () => UpdatePool(include: false));
        applyRandom = Button(words["ApplyRandomFromPool"], () =>
        {

            if (viewModel.TargetMonitorId is { } monitorId)
            {
                viewModel.ApplyRandomFromPool(monitorId);
            }
        });

        count = Label("");
        actions.Children.Add(add);
        actions.Children.Add(remove);
        actions.Children.Add(applyRandom);
        actions.Children.Add(count);
        Children.Add(actions);
        Children.Add(Label(words["MonitorPoolHelp"]));

        AddFilter(GalleryMonitorFilter.AllImages, "PoolFilterAll");
        AddFilter(GalleryMonitorFilter.InPool, "PoolFilterIn");
        AddFilter(GalleryMonitorFilter.OutsidePool, "PoolFilterOut");
        filters.SelectedIndex = (int)viewModel.Filter;

        monitors.SelectionChanged += (_, _) =>
        {

            if (!loadingMonitors && monitors.SelectedItem is MonitorTarget target)
            {
                viewModel.SelectMonitor(target.Id);
                render();
            }
        };

        filters.SelectionChanged += (_, _) =>
        {

            if (filters.SelectedItem is ComboBoxItem { Tag: GalleryMonitorFilter filter })
            {
                viewModel.SetMonitorFilter(filter);
                render();
            }
        };

        Refresh();
    }

    public void SetMonitors(IReadOnlyList<MonitorTarget> connected)
    {
        loadingMonitors = true;
        var all = new MonitorTarget(null, words["All"]);
        monitors.Items.Add(all);

        foreach (var monitor in connected)
        {
            monitors.Items.Add(monitor);
        }

        string? targetId = viewModel.TargetMonitorId;
        var target = connected.FirstOrDefault(monitor => string.Equals(monitor.Id, targetId, StringComparison.OrdinalIgnoreCase));
        targetAvailable = target is not null;
        monitors.SelectedItem = target ?? all;
        monitors.IsEnabled = connected.Count > 0;
        loadingMonitors = false;

        if (target is null && targetId is not null)
        {
            viewModel.SelectMonitor(null);
            render();

            return;
        }

        Refresh();
    }

    public void SetSelectedImages(IReadOnlyList<string> paths)
    {
        selectedImages = paths;
        Refresh();
    }

    private void AddFilter(GalleryMonitorFilter filter, string label) =>
        filters.Items.Add(new ComboBoxItem { Content = words[label], Tag = filter });

    private void UpdatePool(bool include)
    {

        if (viewModel.TargetMonitorId is not { } monitorId)
        {

            return;
        }

        viewModel.ChangeMonitorPool(monitorId, selectedImages, include);

        if (viewModel.Filter == GalleryMonitorFilter.AllImages)
        {
            Refresh();
            poolChanged();
        }
        else
        {
            render();
        }
    }

    private void Refresh()
    {
        string? target = viewModel.TargetMonitorId;
        filters.IsEnabled = targetAvailable;
        add.IsEnabled = targetAvailable && target is not null && selectedImages.Any(path => !viewModel.IsInMonitorPool(target, path));
        remove.IsEnabled = targetAvailable && target is not null && selectedImages.Any(path => viewModel.IsInMonitorPool(target, path));
        int imageCount = target is null ? 0 : viewModel.GetMonitorPoolCount(target);
        applyRandom.IsEnabled = targetAvailable && imageCount > 0;
        count.Visibility = target is null ? Visibility.Collapsed : Visibility.Visible;
        count.Text = string.Format(CultureInfo.GetCultureInfo(words.Code), words["MonitorPoolCount"], imageCount);
    }
}
