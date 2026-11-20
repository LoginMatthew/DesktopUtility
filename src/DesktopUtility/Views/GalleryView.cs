using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using static DesktopUtility.PresentationControls.Controls;

namespace DesktopUtility.Views;

internal sealed class GalleryView(Window owner, GalleryViewModel viewModel, Language words, Action render)
{
    private CancellationTokenSource? thumbnailLoad;
    public void CancelLoading() => thumbnailLoad?.Cancel();

    public FrameworkElement Build()
    {
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

        var operations = new WrapPanel();

        DockPanel.SetDock(operations, Dock.Bottom);
        layout.Children.Add(operations);

        var monitors = new ComboBox { MinWidth = 115 };
        monitors.Items.Add(new MonitorTarget(null, words["All"]));

        _ = PopulateMonitorsAsync(monitors);
        monitors.SelectedIndex = 0;

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
        operations.Children.Add(monitors);

        var items = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 9),
            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)))
        };

        items.SetResourceReference(Control.ForegroundProperty, "AppText");
        ScrollViewer.SetHorizontalScrollBarVisibility(items, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(items, ScrollBarVisibility.Auto);

        string? CurrentPath() => (items.SelectedItem as ListBoxItem)?.Tag as string;
        var apply = Button(words["Apply"], () => viewModel.Apply(CurrentPath(), (monitors.SelectedItem as MonitorTarget)?.Id));
        var favorite = Button(words["Favorite"], () => viewModel.SetFavorite(CurrentPath()));

        apply.IsEnabled = favorite.IsEnabled = false;
        items.SelectionChanged += (_, _) => apply.IsEnabled = favorite.IsEnabled = CurrentPath() is not null;
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
            var include = CheckBox("✓", viewModel.Includes(path), value => viewModel.Include(path, value));
            include.ToolTip = words["Include"];
            System.Windows.Automation.AutomationProperties.SetName(include, words["Include"]);
            card.Children.Add(include);
            items.Items.Add(new ListBoxItem { Content = card, Tag = path, Margin = new Thickness(0, 0, 7, 7) });
            thumbnails.Add((path, thumbnail, details));
        }

        layout.Children.Add(items);
        thumbnailLoad = new CancellationTokenSource();
        _ = LoadThumbnailsAsync(thumbnails, thumbnailLoad);

        return layout;
    }

    private async Task PopulateMonitorsAsync(ComboBox monitors)
    {

        foreach (var monitor in await viewModel.GetMonitorsAsync())
        {
            monitors.Items.Add(monitor);
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
