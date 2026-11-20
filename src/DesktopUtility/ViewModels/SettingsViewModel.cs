using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopUtility.ViewModels;

internal sealed class SettingsViewModel(
    WallpaperSession session,
    Language words,
    Func<bool> readStartup,
    Func<bool, bool> writeStartup,
    Action changeLanguage,
    Action applyAppearance) : INotifyPropertyChanged
{
    public Language Words => words;
    public IReadOnlyList<string> Languages => Language.Names;

    public int LanguageIndex
    {
        get => words.Index;
        set
        {

            if (value < 0 || value == words.Index)
            {

                return;
            }

            words.Select(value);
            session.Settings.Language = words.Code;
            session.Save();
            changeLanguage();
        }
    }

    public bool StartupEnabled
    {
        get => readStartup();
        set
        {
            writeStartup(value);
            session.NotifyState();
            Notify();
        }
    }

    public bool SeparateMonitors
    {
        get => session.Settings.SeparateMonitors;
        set => session.SetSeparateMonitors(value);
    }

    public double AccentHue
    {
        get => session.Settings.AccentHue;
        set => UpdateAppearance(value, hue => session.Settings.AccentHue = hue);
    }

    public double TextHue
    {
        get => session.Settings.TextHue;
        set => UpdateAppearance(value, hue => session.Settings.TextHue = hue);
    }

    public double SurfaceHue
    {
        get => session.Settings.SurfaceHue;
        set => UpdateAppearance(value, hue => session.Settings.SurfaceHue = hue);
    }

    public double Opacity
    {
        get => session.Settings.Opacity;
        set => UpdateAppearance(value, opacity => session.Settings.Opacity = opacity);
    }

    public double PanelOpacity
    {
        get => session.Settings.PanelOpacity;
        set => UpdateAppearance(value, opacity => session.Settings.PanelOpacity = opacity);
    }

    public void ResetAppearance()
    {
        var defaults = new Settings();
        session.Settings.AccentHue = defaults.AccentHue;
        session.Settings.TextHue = defaults.TextHue;
        session.Settings.SurfaceHue = defaults.SurfaceHue;
        session.Settings.Opacity = defaults.Opacity;
        session.Settings.PanelOpacity = defaults.PanelOpacity;
        applyAppearance();
        session.Save();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void UpdateAppearance(double value, Action<double> update, [CallerMemberName] string? property = null)
    {
        update(value);
        applyAppearance();
        session.Save();
        Notify(property);
    }

    private void Notify([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
