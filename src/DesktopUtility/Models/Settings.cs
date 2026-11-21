using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.Models;

internal sealed class Settings
{
    public string Language { get; set; } = "en";
    public string Folder { get; set; } = "";
    public string? Favorite { get; set; }
    public HashSet<string> Selected { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> MonitorAssignments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> MonitorFavorites { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, HashSet<string>> MonitorPools { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool SelectedOnly { get; set; }
    public bool SeparateMonitors { get; set; }
    public bool RotationEnabled { get; set; }
    public bool ShowMonitoringCharts { get; set; }
    public string? PresentMonPath { get; set; }
    public bool MonitoringEnabled { get; set; }
    public SystemMetric MonitoringMetrics { get; set; } = SystemMetric.All;
    public bool MonitorAllDisks { get; set; } = true;
    public HashSet<string> MonitoredDriveRoots { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int IntervalMinutes { get; set; } = 30;
    public double AccentHue { get; set; } = 165;
    public double TextHue { get; set; } = 210;
    public double SurfaceHue { get; set; } = 214;
    public double PanelOpacity { get; set; } = 0.92;
    public double Opacity { get; set; } = 1;
}
