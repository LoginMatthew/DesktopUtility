
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
    private readonly MonitoringViewModel monitoring;
    private string activePage = "Gallery";

    internal MainWindow(WallpaperSession session, Language words, SettingsViewModel settings, GalleryViewModel galleryModel, MonitoringViewModel monitoring)
    {
        this.session = session;
        this.words = words;
        this.settings = settings;
        this.galleryModel = galleryModel;
        this.monitoring = monitoring;
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

        StateChanged += (_, _) => UpdateMonitoringVisibility();
        IsVisibleChanged += (_, _) =>
        {

            if (IsVisible)
            {
                Render();
            }
            else
            {
                gallery.CancelLoading();
                monitoring.SetViewing(false);
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
        UpdateMonitoringVisibility();
        UpdateCaptionLabels();
        Navigation.Children.Clear();

        foreach (string page in new[] { "Gallery", "Monitoring", "Settings", "About" })
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

        HelpTopic? topic = activePage switch
        {
            "Gallery" => HelpTopic.Gallery,
            "Settings" => HelpTopic.Settings,
            "Monitoring" => HelpTopic.Monitoring,
            _ => null
        };

        if (topic is { } pageTopic)
        {
            Navigation.Children.Add(Button(words["PageHelp"], () => ShowPageHelp(pageTopic)));
        }

        ContentHost.Children.Clear();
        ContentHost.Children.Add(activePage switch
        {
            "Monitoring" => new MonitoringView(monitoring),
            "Settings" => new SettingsView(settings, session),
            "About" => new AboutView(new AboutViewModel(words)),
            _ => gallery.Build()
        });
        status.Refresh();
    }

    private void UpdateMonitoringVisibility() =>
        monitoring.SetViewing(IsVisible && WindowState != WindowState.Minimized && activePage == "Monitoring");

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
