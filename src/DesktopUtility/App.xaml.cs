using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using DesktopUtility.Infrastructure.Persistence;
using DesktopUtility.Infrastructure.Startup;
using DesktopUtility.Infrastructure.Wallpaper;


using DesktopUtility.Themes;
using Forms = System.Windows.Forms;

namespace DesktopUtility;

public partial class App : Application
{
    private readonly Language words = new();
    private readonly SettingsStore store = new(Path.Combine(AppContext.BaseDirectory, "settings.json"));
    private SettingsPersistence? persistence;
    private WallpaperSession? session;
    private DispatcherTimer? rotationTimer;
    private WindowsWallpaperService? wallpaper;
    private MainWindow? dashboard;
    private Forms.NotifyIcon? trayIcon;
    private System.Drawing.Icon? trayArtwork;
    private Window? quickPanel;
    private FolderLoader? folderLoader;
    private Mutex? instance;
    private bool ownsInstance;

    private bool exiting;

    private bool StartupEnabled { get; set; }

    protected override void OnStartup(StartupEventArgs args)
    {
        base.OnStartup(args);
        instance = new Mutex(initiallyOwned: true, @"Local\DesktopUtility.Tray", out ownsInstance);

        if (!ownsInstance)
        {
            Shutdown();

            return;
        }

        string? message = null;
        Settings settings;
        try
        {
            settings = store.Load();
            StartupEnabled = WindowsStartup.IsEnabled();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or SecurityException)
        {
            settings = new Settings();
            message = "SettingsError";
            Trace.TraceWarning(error.ToString());
        }

        wallpaper = new WindowsWallpaperService();
        session = new WallpaperSession(settings, wallpaper, TimeProvider.System) { MessageKey = message };
        rotationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        rotationTimer.Tick += OnRotationTick;
        rotationTimer.Start();
        persistence = new SettingsPersistence(store, session);
        words.Select(settings.Language);
        var appearance = new SettingsViewModel(session, words, () => StartupEnabled, SetStartup, ChangeLanguage, ApplyAppearance);
        folderLoader = new FolderLoader(session, () => dashboard!.FolderChanged());
        var gallery = new GalleryViewModel(session, folder => _ = folderLoader.LoadAsync(folder));
        dashboard = new MainWindow(session, words, appearance, gallery);
        dashboard.Closing += HideDashboard;
        MainWindow = dashboard;
        ApplyAppearance();

        trayArtwork = LoadTrayArtwork();
        trayIcon = new Forms.NotifyIcon
        {
            Icon = trayArtwork,
            Text = "Desktop Utility",
            Visible = true
        };

        trayIcon.MouseClick += (_, click) =>
        {

            if (click.Button == Forms.MouseButtons.Right)
            {
                ToggleQuickPanel();
            }

            else if (click.Button == Forms.MouseButtons.Left)
            {
                ShowDashboard();
            }
        };

        _ = folderLoader.LoadAsync(settings.Folder);

        if (args.Args.Contains("--open", StringComparer.OrdinalIgnoreCase))
        {
            ShowDashboard();
        }
    }

    private static System.Drawing.Icon LoadTrayArtwork()
    {
        using var resource = GetResourceStream(new Uri("/Assets/DesktopUtility.ico", UriKind.Relative)).Stream;
        using var icon = new System.Drawing.Icon(resource);

        return (System.Drawing.Icon)icon.Clone();
    }

    private void OnRotationTick(object? sender, EventArgs args) => _ = session?.TickAsync();

    private void ApplyAppearance() => ThemeResources.Apply(Resources, session!.Settings);
    internal bool SetStartup(bool enabled)
    {
        try
        {
            WindowsStartup.SetEnabled(enabled);
            StartupEnabled = enabled;
            session!.MessageKey = null;

            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            session!.MessageKey = "StartupError";
            Trace.TraceWarning(error.ToString());

            return false;
        }
    }

    private void ChangeLanguage()
    {
        dashboard!.Render();
        quickPanel?.Close();
    }

    private void ShowDashboard() => TrayWindowNavigation.OpenDashboard(dashboard!, quickPanel);

    private void HideDashboard(object? sender, CancelEventArgs args)
    {

        if (!exiting)
        {
            args.Cancel = true;
            dashboard!.Hide();
        }
    }

    private void ToggleQuickPanel()
    {

        if (quickPanel is not null)
        {
            quickPanel.Close();

            return;
        }

        var window = new QuickPanelWindow(session!, words, ShowDashboard, Shutdown);
        window.Closed += (_, _) => quickPanel = null;
        quickPanel = window;
        window.Show();
        window.Activate();
    }

    protected override void OnExit(ExitEventArgs args)
    {
        exiting = true;
        folderLoader?.Dispose();
        persistence?.Dispose();
        rotationTimer?.Stop();
        session?.Dispose();
        wallpaper?.Dispose();
        trayIcon?.Dispose();
        trayArtwork?.Dispose();

        if (ownsInstance)
        {
            instance?.ReleaseMutex();
        }

        instance?.Dispose();
        base.OnExit(args);
    }
}
