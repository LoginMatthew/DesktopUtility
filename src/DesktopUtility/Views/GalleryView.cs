using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using static DesktopUtility.PresentationControls.Controls;

namespace DesktopUtility.Views;

internal sealed class GalleryView(Window owner, GalleryViewModel viewModel, Language words, Action render)
{
    private CancellationTokenSource? thumbnailLoad;
    private int viewVersion;
    private string[] highlightedPaths = [];

    public void CancelLoading()
    {
        viewVersion++;
        thumbnailLoad?.Cancel();
    }

    public FrameworkElement Build()
    {
        int currentVersion = ++viewVersion;
        var previousSelection = highlightedPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var layout = new DockPanel();
        var header = new StackPanel();

        DockPanel.SetDock(header, Dock.Top);
        layout.Children.Add(header);

        var toolbar = new WrapPanel();
        toolbar.Children.Add(Button(words["Folder"], ChooseFolder));
        toolbar.Children.Add(Button(words["Refresh"], viewModel.Refresh));
        toolbar.Children.Add(Button(words["ApplyFavorite"], viewModel.ApplyFavorite));
        header.Children.Add(toolbar);

        if (!string.IsNullOrEmpty(viewModel.Folder))
        {
            header.Children.Add(new TextBlock { Text = viewModel.Folder, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 3, 0, 8) });
        }

        header.Children.Add(CheckBox(words["Selected"], viewModel.SelectedOnly, value => viewModel.SelectedOnly = value));

        if (!viewModel.HasImages)
        {
            layout.Children.Add(Label(words["Empty"]));

            return layout;
        }

        var selectionChecks = new List<(string Path, CheckBox Control)>();
        void SetAllSelected(bool selected)
        {
            viewModel.SetAllSelected(selected);

            foreach (var (path, checkBox) in selectionChecks)
            {
                checkBox.IsChecked = viewModel.Includes(path);
            }
        }

        toolbar.Children.Add(Button(words["SelectAllImages"], () => SetAllSelected(true)));
        toolbar.Children.Add(Button(words["ClearSelectedImages"], () => SetAllSelected(false)));

        IReadOnlyList<MonitorTarget> visibleMonitors = [];
        var monitorBadges = new List<(string Path, TextBlock Label)>();
        var connectedMonitors = viewModel.GetMonitorsAsync();
        var monitorAssignments = new MonitorAssignmentsView(viewModel, words, connectedMonitors, id =>
        {
            viewModel.SelectMonitor(id);
            viewModel.SetMonitorFilter(GalleryMonitorFilter.InPool);
            render();
        });

        var poolControls = new MonitorPoolControls(viewModel, words, render, RefreshMonitorSelections);
        header.Children.Add(poolControls);
        header.Children.Add(monitorAssignments);
        monitorAssignments.SelectionsChanged += UpdateMonitorBadges;

        var operations = new WrapPanel();

        DockPanel.SetDock(operations, Dock.Bottom);
        layout.Children.Add(operations);

        int lastPage = viewModel.LastPage;

        var previous = Button("‹", () =>
        {
            viewModel.ChangePage(-1);
            render();
        });

        previous.ToolTip = words["PreviousPage"];
        previous.IsEnabled = viewModel.PageIndex > 0;

        var next = Button("›", () =>
        {
            viewModel.ChangePage(1);
            render();
        });

        next.ToolTip = words["NextPage"];
        next.IsEnabled = viewModel.PageIndex < lastPage;

        operations.Children.Add(previous);
        operations.Children.Add(Label($"{viewModel.PageIndex + 1} / {lastPage + 1}   "));
        operations.Children.Add(next);

        var items = new ListBox
        {
            Background = Brushes.Transparent,
            SelectionMode = SelectionMode.Extended,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 9),
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)))
        };

        items.SetResourceReference(Control.ForegroundProperty, "AppText");
        ScrollViewer.SetHorizontalScrollBarVisibility(items, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(items, ScrollBarVisibility.Auto);

        string? CurrentPath() => items.SelectedItems.Count == 1 ? (items.SelectedItem as ListBoxItem)?.Tag as string : null;
        var apply = Button(words["Apply"], () =>
        {
            viewModel.Apply(CurrentPath(), viewModel.TargetMonitorId);
            monitorAssignments.UpdateRows();
            UpdateMonitorBadges();
        });

        var favoriteBadges = new List<(string Path, TextBlock Label)>();
        var favoriteSummary = Label("");
        favoriteSummary.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
        header.Children.Insert(header.Children.IndexOf(monitorAssignments), favoriteSummary);
        var favorite = Button(words["Favorite"], () =>
        {
            viewModel.SetFavorite(CurrentPath());
            UpdateFavorite();
        });

        void UpdateFavorite()
        {
            string? path = viewModel.FavoritePath;
            favoriteSummary.Text = path is null ? "" : $"★ {words["FavoriteBadge"]} · {Path.GetFileName(path)}";
            favoriteSummary.ToolTip = path;
            favoriteSummary.Visibility = path is null ? Visibility.Collapsed : Visibility.Visible;

            foreach (var (imagePath, badge) in favoriteBadges)
            {
                badge.Visibility = viewModel.IsFavorite(imagePath) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        apply.IsEnabled = favorite.IsEnabled = false;
        items.SelectionChanged += (_, _) =>
        {
            string? path = CurrentPath();
            apply.IsEnabled = favorite.IsEnabled = path is not null;
            monitorAssignments.SetSelectedImage(path);
            highlightedPaths = items.SelectedItems.Cast<ListBoxItem>().Select(item => (string)item.Tag).ToArray();
            poolControls.SetSelectedImages(highlightedPaths);
        };

        operations.Children.Add(apply);
        operations.Children.Add(favorite);

        var thumbnails = new List<(string Path, Image View, TextBlock Details)>();

        foreach (string path in viewModel.PageImages)
        {
            var thumbnail = new Image { Height = 99, Stretch = Stretch.UniformToFill };
            var card = new StackPanel { Width = 153 };
            card.Children.Add(new Border { Child = thumbnail, Background = Brushes.Black, ClipToBounds = true });
            card.Children.Add(new TextBlock { Text = Path.GetFileName(path), ToolTip = path, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 5, 0, 1) });
            var details = new TextBlock { Text = words["PreviewLoading"], FontSize = 12, TextWrapping = TextWrapping.Wrap };
            card.Children.Add(details);
            var favoriteBadge = new TextBlock { Text = $"★ {words["FavoriteBadge"]}", FontWeight = FontWeights.SemiBold };
            favoriteBadge.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
            card.Children.Add(favoriteBadge);
            var monitorBadge = new TextBlock { FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
            monitorBadge.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
            card.Children.Add(monitorBadge);
            monitorBadges.Add((path, monitorBadge));
            favoriteBadges.Add((path, favoriteBadge));
            var include = CheckBox("✓", viewModel.Includes(path), value => viewModel.Include(path, value));
            include.ToolTip = words["Include"];
            System.Windows.Automation.AutomationProperties.SetName(include, words["Include"]);
            card.Children.Add(include);
            selectionChecks.Add((path, include));
            var item = new ListBoxItem { Content = card, Tag = path, Margin = new Thickness(0, 0, 7, 7) };
            items.Items.Add(item);

            if (previousSelection.Contains(path))
            {
                items.SelectedItems.Add(item);
            }

            thumbnails.Add((path, thumbnail, details));
        }

        UpdateFavorite();
        _ = LoadGalleryMonitorsAsync();
        layout.Children.Add(items.Items.Count > 0 ? items : Label(words["NoMonitorFilterResults"]));
        thumbnailLoad = new CancellationTokenSource();
        _ = LoadThumbnailsAsync(thumbnails, thumbnailLoad);

        return layout;
        async Task LoadGalleryMonitorsAsync()
        {
            await Dispatcher.Yield(DispatcherPriority.Loaded);
            visibleMonitors = await connectedMonitors;

            if (currentVersion != viewVersion)
            {

                return;
            }

            poolControls.SetMonitors(visibleMonitors);
            UpdateMonitorBadges();
        }

        void RefreshMonitorSelections()
        {
            monitorAssignments.UpdateRows();
            UpdateMonitorBadges();
        }

        void UpdateMonitorBadges()
        {

            foreach (var (path, label) in monitorBadges)
            {
                string assignment = FormatMonitorLabels(path, viewModel.GetWallpaperAssignment, "MonitorBadge");
                string favorite = FormatMonitorLabels(path, viewModel.GetMonitorFavorite, "MonitorFavoriteBadge");
                string pools = FormatMonitorLabels(path, id => viewModel.IsInMonitorPool(id, path) ? path : null, "MonitorPoolBadge");
                label.Text = string.Join(Environment.NewLine, new[] { assignment, favorite, pools }.Where(text => text.Length > 0));
                label.Visibility = label.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        string FormatMonitorLabels(string path, Func<string, string?> findImage, string labelKey)
        {
            var matchingMonitors = visibleMonitors
                .Where(monitor => monitor.Id is { } id && string.Equals(findImage(id), path, StringComparison.OrdinalIgnoreCase))
                .Select(monitor => monitor.Number is { } number
                    ? string.Format(CultureInfo.GetCultureInfo(words.Code), words[labelKey], number)
                    : labelKey == "MonitorFavoriteBadge" ? $"{words["FavoriteBadge"]} · {monitor.Name}" : monitor.Name);
            string text = string.Join(" · ", matchingMonitors);

            return labelKey == "MonitorFavoriteBadge" && text.Length > 0 ? $"★ {text}" : text;
        }
    }

    private async Task LoadThumbnailsAsync(List<(string Path, Image View, TextBlock Details)> thumbnails, CancellationTokenSource request)
    {
        try
        {

            for (int index = 0; index < thumbnails.Count; index += 3)
            {
                var batch = thumbnails.Skip(index).Take(3).ToArray();
                var previews = await Task.WhenAll(batch.Select(item => viewModel.ReadPreviewAsync(item.Path, request.Token)));
                request.Token.ThrowIfCancellationRequested();

                for (int position = 0; position < batch.Length; position++)
                {
                    var preview = previews[position];
                    var (_, thumbnail, details) = batch[position];
                    thumbnail.Source = preview.Thumbnail;
                    var culture = CultureInfo.GetCultureInfo(words.Code);
                    string resolution = preview.HasDimensions
                        ? $"{preview.PixelWidth} × {preview.PixelHeight} px"
                        : words["UnknownResolution"];
                    string fileSize = preview.FileSizeBytes is long bytes
                        ? GalleryViewModel.FormatFileSize(bytes, culture)
                        : words["UnknownFileSize"];
                    details.Text = $"{resolution}{Environment.NewLine}{fileSize}";

                    if (preview.FileSizeBytes is long exactBytes)
                    {
                        details.ToolTip = $"{resolution}{Environment.NewLine}{exactBytes.ToString("N0", culture)} B";
                    }
                }
            }
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            // Cancellation is expected when the gallery changes or closes.
        }
        finally
        {

            if (ReferenceEquals(thumbnailLoad, request))
            {
                thumbnailLoad = null;
            }

            request.Dispose();
        }
    }

    private void ChooseFolder()
    {
        var picker = new OpenFolderDialog { Title = words["Folder"] };

        if (Directory.Exists(viewModel.Folder))
        {
            picker.InitialDirectory = viewModel.Folder;
        }

        if (picker.ShowDialog(owner) == true)
        {
            viewModel.OpenFolder(picker.FolderName);
        }
    }

}
