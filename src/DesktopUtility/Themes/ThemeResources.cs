using System.Windows;
using System.Windows.Media;

namespace DesktopUtility.Themes;

internal static class ThemeResources
{
    public static void Apply(ResourceDictionary resources, Settings settings)
    {
        resources["Accent"] = Controls.Hue(settings.AccentHue, 0.64, 0.68);
        resources["AppText"] = Controls.Hue(settings.TextHue, 0.38, 0.90);
        resources["WindowSurface"] = Surface(settings.SurfaceHue, 0.10, settings.Opacity);
        resources["PanelSurface"] = Surface(settings.SurfaceHue, 0.17, settings.PanelOpacity * settings.Opacity);
        resources["ControlSurface"] = Surface(settings.SurfaceHue, 0.23, 1);
        resources["SelectedControlSurface"] = Surface(settings.AccentHue, 0.24, 1);
        resources["ChromeStroke"] = Surface(settings.AccentHue, 0.52, 0.66);
    }

    private static SolidColorBrush Surface(double hue, double lightness, double opacity)
    {
        var color = Controls.Hue(hue, 0.25, lightness).Color;
        color.A = (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255);
        var brush = new SolidColorBrush(color);
        brush.Freeze();

        return brush;
    }
}
