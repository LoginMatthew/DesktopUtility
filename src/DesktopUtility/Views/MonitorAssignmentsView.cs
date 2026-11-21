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
    private readonly Action<string> openPool;
    private readonly StackPanel rows = new();
    private readonly List<MonitorRow> monitorRows = [];
    private readonly Button apply;
    private string? selectedImage;

    public event Action? SelectionsChanged;

    public MonitorAssignmentsView(GalleryViewModel viewModel, Language words, Task<IReadOnlyList<MonitorTarget>> connectedMonitors, Action<string> openPool)
    {
        this.viewModel = viewModel;
        this.words = words;
        this.connectedMonitors = connectedMonitors;
        this.openPool = openPool;
        Header = words["MonitorAssignments"];
        Margin = new Thickness(0, 4, 0, 5);

        var layout = new StackPanel { Margin = new Thickness(10, 4, 4, 4) };
        var scroll = new ScrollViewer
        {
            Content = rows,
            MaxHeight = 210,
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
            row.SetFavorite.IsEnabled = path is not null;
        }
    }

    private async void LoadMonitors(object sender, RoutedEventArgs args)
    {
        Loaded -= LoadMonitors;
        var monitors = await connectedMonitors;
        rows.Children.Clear();
        Header = $"{words["MonitorAssignments"]} ({monitors.Count})";

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

        UpdateRows();
    }

    private void AddRow(string id, string name)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        var heading = Label(name);
        heading.FontWeight = FontWeights.SemiBold;
        row.Children.Add(heading);

        var assign = Button(words["AssignSelectedImage"], () => ChangeSelectedImage(path => viewModel.AssignWallpaper(id, path)));
        var clear = Button(words["ClearAssignment"], () => ChangeSelection(() => viewModel.ClearWallpaperAssignment(id)));
        var setFavorite = Button(words["SetMonitorFavorite"], () => ChangeSelectedImage(path => viewModel.SetMonitorFavorite(id, path)));
        var clearFavorite = Button(words["ClearMonitorFavorite"], () => ChangeSelection(() => viewModel.ClearMonitorFavorite(id)));

        assign.IsEnabled = setFavorite.IsEnabled = selectedImage is not null;

        var image = Label("");
        AddSelector(row, image, assign, clear);
        var favorite = Label("");
        favorite.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
        AddSelector(row, favorite, setFavorite, clearFavorite);

        var poolControls = new WrapPanel();
        var poolCount = Label("");
        poolControls.Children.Add(poolCount);
        poolControls.Children.Add(Button(words["OpenMonitorPool"], () => openPool(id)));
        row.Children.Add(poolControls);

        monitorRows.Add(new MonitorRow(id, assign, image, clear, setFavorite, favorite, clearFavorite, poolCount));
        rows.Children.Add(row);
    }

    private static void AddSelector(StackPanel row, TextBlock description, Button set, Button clear)
    {
        var controls = new WrapPanel();
        description.MaxWidth = 250;
        controls.Children.Add(description);
        controls.Children.Add(set);
        controls.Children.Add(clear);
        row.Children.Add(controls);
    }

    private void ChangeSelectedImage(Action<string> change)
    {

        if (selectedImage is { } path)
        {
            ChangeSelection(() => change(path));
        }
    }

    private void ChangeSelection(Action change)
    {
        change();
        UpdateRows();
        SelectionsChanged?.Invoke();
    }

    public void UpdateRows()
    {
        bool hasAssignment = false;

        foreach (var row in monitorRows)
        {
            string? path = viewModel.GetWallpaperAssignment(row.Id);
            row.Image.Text = path is null ? words["NoAssignedImage"] : Path.GetFileName(path);
            row.Image.ToolTip = path;
            row.Clear.IsEnabled = path is not null;
            hasAssignment |= path is not null;

            string? favorite = viewModel.GetMonitorFavorite(row.Id);
            row.Favorite.Text = favorite is null ? words["NoMonitorFavorite"]
                : $"★ {words["FavoriteBadge"]} · {Path.GetFileName(favorite)}";
            row.Favorite.ToolTip = favorite;
            row.ClearFavorite.IsEnabled = favorite is not null;
            row.PoolCount.Text = string.Format(System.Globalization.CultureInfo.GetCultureInfo(words.Code),
                words["MonitorPoolCount"], viewModel.GetMonitorPoolCount(row.Id));
        }

        apply.IsEnabled = hasAssignment;
    }

    private sealed record MonitorRow(string Id, Button Assign, TextBlock Image, Button Clear,
        Button SetFavorite, TextBlock Favorite, Button ClearFavorite, TextBlock PoolCount);
}
