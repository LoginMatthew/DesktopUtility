
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using static DesktopUtility.PresentationControls.Controls;

namespace DesktopUtility.Views;

public partial class MainWindow : Window
{
    private readonly WallpaperSession session;
    private readonly RotationStatusViewModel status;
    private readonly Language words;
    private readonly SettingsViewModel settings;
    private readonly GalleryViewModel galleryModel;
    private readonly GalleryView gallery;
    private string activePage = "Gallery";

    internal MainWindow(WallpaperSession session, Language words, SettingsViewModel settings, GalleryViewModel galleryModel)
    {
        this.session = session;
        this.words = words;
        this.settings = settings;
        this.galleryModel = galleryModel;
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            WindowMaximization.Attach(this);
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        };

        gallery = new GalleryView(this, galleryModel, words, Render);
        status = new RotationStatusViewModel(session, words);
        DataContext = status;
        Closed += (_, _) =>
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            status.Dispose();
        };

        IsVisibleChanged += (_, _) =>
        {

            if (IsVisible)
            {
                Render();
            }
            else
            {
                gallery.CancelLoading();
            }
        };
    }

    internal void FolderChanged()
    {
        galleryModel.ResetPage();

        if (IsVisible)
        {
            Render();
        }
    }

    internal void Render()
    {
        gallery.CancelLoading();
        UpdateCaptionLabels();
        Navigation.Children.Clear();

        foreach (string page in new[] { "Gallery", "Settings", "About" })
        {
            var button = Button(words[page], () =>
            {
                activePage = page;
                Render();
            });

            if (activePage == page)
            {
                button.SetResourceReference(BorderBrushProperty, "Accent");
                button.SetResourceReference(BackgroundProperty, "SelectedControlSurface");
            }

            Navigation.Children.Add(button);
        }

        if (activePage is "Gallery" or "Settings")
        {
            HelpTopic topic = activePage == "Gallery" ? HelpTopic.Gallery : HelpTopic.Settings;
            Navigation.Children.Add(Button(words["PageHelp"], () => ShowPageHelp(topic)));
        }

        ContentHost.Children.Clear();
        ContentHost.Children.Add(activePage switch
        {
            "Settings" => new SettingsView(settings, session),
            "About" => new AboutView(new AboutViewModel(words)),
            _ => gallery.Build()
        });
        status.Refresh();
    }

    private void ShowPageHelp(HelpTopic topic)
    {
        var help = new PageHelpWindow(this, new PageHelpViewModel(words, topic));
        help.ShowDialog();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs args)
    {

        if (Dispatcher.HasShutdownStarted)
        {

            return;
        }

        _ = Dispatcher.InvokeAsync(() =>
        {

            if (IsVisible && activePage == "Gallery")
            {
                Render();
            }
        });
    }

    private void UpdateCaptionLabels()
    {
        MinimizeButton.ToolTip = words["Minimize"];
        MaximizeButton.ToolTip = words["Maximize"];
        CloseButton.ToolTip = words["Close"];
    }

    private void Minimize(object sender, RoutedEventArgs args) => SystemCommands.MinimizeWindow(this);
    private void MaximizeOrRestore(object sender, RoutedEventArgs args)
    {

        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void CloseToTray(object sender, RoutedEventArgs args) => Close();
}
