using System.IO;
using System.Windows;
using System.Windows.Controls;
using static DesktopUtility.PresentationControls.Controls;

namespace DesktopUtility.Views;

internal sealed class MonitorAssignmentsView : Expander
{
    private readonly GalleryViewModel viewModel;
    private readonly Language words;
    private readonly Task<IReadOnlyList<MonitorTarget>> connectedMonitors;
    private readonly StackPanel rows = new();
    private readonly List<MonitorRow> monitorRows = [];
    private readonly Button apply;
    private string? selectedImage;

    public MonitorAssignmentsView(GalleryViewModel viewModel, Language words, Task<IReadOnlyList<MonitorTarget>> connectedMonitors)
    {
        this.viewModel = viewModel;
        this.words = words;
        this.connectedMonitors = connectedMonitors;
        Header = words["MonitorAssignments"];
        Margin = new Thickness(0, 4, 0, 5);

        var layout = new StackPanel { Margin = new Thickness(10, 4, 4, 4) };
        var scroll = new ScrollViewer
        {
            Content = rows,
            MaxHeight = 160,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        layout.Children.Add(Label(words["MonitorAssignmentHelp"]));
        layout.Children.Add(scroll);
        apply = Button(words["ApplyMonitorAssignments"], () => viewModel.ApplyMonitorAssignments(monitorRows.Select(row => row.Id)));
        apply.HorizontalAlignment = HorizontalAlignment.Left;
        apply.IsEnabled = false;
        layout.Children.Add(apply);
        Content = layout;
        rows.Children.Add(Label(words["LoadingMonitors"]));
        Loaded += LoadMonitors;
    }

    public void SetSelectedImage(string? path)
    {
        selectedImage = path;

        foreach (var row in monitorRows)
        {
            row.Assign.IsEnabled = path is not null;
        }
    }

    private async void LoadMonitors(object sender, RoutedEventArgs args)
    {
        Loaded -= LoadMonitors;
        var monitors = await connectedMonitors;
        rows.Children.Clear();

        foreach (var monitor in monitors)
        {

            if (monitor.Id is { } id)
            {
                AddRow(id, monitor.Name);
            }
        }

        if (monitorRows.Count == 0)
        {
            rows.Children.Add(Label(words["NoMonitors"]));
        }

        UpdateApplyButton();
    }

    private void AddRow(string id, string name)
    {
        var row = new WrapPanel();
        row.Children.Add(Label(name));
        var image = Label("");
        image.MaxWidth = 220;
        row.Children.Add(image);

        var clear = new Button { Content = words["ClearAssignment"] };
        var assign = Button(words["AssignSelectedImage"], () =>
        {

            if (selectedImage is { } path)
            {
                viewModel.AssignWallpaper(id, path);
                UpdateRow();
            }
        });

        clear.Click += (_, _) =>
        {
            viewModel.ClearWallpaperAssignment(id);
            UpdateRow();
        };

        assign.IsEnabled = selectedImage is not null;
        row.Children.Add(assign);
        row.Children.Add(clear);
        monitorRows.Add(new MonitorRow(id, assign));
        rows.Children.Add(row);
        UpdateRow();

        void UpdateRow()
        {
            string? path = viewModel.GetWallpaperAssignment(id);
            image.Text = path is null ? words["NoAssignedImage"] : Path.GetFileName(path);
            image.ToolTip = path;
            clear.IsEnabled = path is not null;
            UpdateApplyButton();
        }
    }

    private void UpdateApplyButton() => apply.IsEnabled = monitorRows.Any(row => viewModel.GetWallpaperAssignment(row.Id) is not null);

    private sealed record MonitorRow(string Id, Button Assign);
}
