namespace DesktopUtility.Models;

internal sealed class Settings
{
    public string Language { get; set; } = "en";
    public string Folder { get; set; } = "";
    public string? Favorite { get; set; }
    public HashSet<string> Selected { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool SelectedOnly { get; set; }
    public bool SeparateMonitors { get; set; }
    public bool RotationEnabled { get; set; }
    public int IntervalMinutes { get; set; } = 30;
    public double AccentHue { get; set; } = 165;
    public double TextHue { get; set; } = 210;
    public double SurfaceHue { get; set; } = 214;
    public double PanelOpacity { get; set; } = 0.92;
    public double Opacity { get; set; } = 1;
}

