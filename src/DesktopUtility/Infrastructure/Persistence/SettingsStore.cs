using System.IO;
using System.Text.Json;

namespace DesktopUtility.Infrastructure.Persistence;

internal sealed class SettingsStore(string path)
{
    public Settings Load()
    {

        if (!File.Exists(path))
        {

            return new Settings();
        }

        var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
        settings.IntervalMinutes = Math.Clamp(settings.IntervalMinutes, 1, 1440);
        settings.Opacity = Math.Clamp(settings.Opacity, 0.55, 1);
        settings.PanelOpacity = Math.Clamp(settings.PanelOpacity, 0, 1);
        settings.Folder ??= "";
        settings.Selected = new HashSet<string>(settings.Selected ?? [], StringComparer.OrdinalIgnoreCase);

        settings.MonitorAssignments = NormalizeMonitorImages(settings.MonitorAssignments);
        settings.MonitorFavorites = NormalizeMonitorImages(settings.MonitorFavorites);
        settings.MonitorPools = NormalizeMonitorPools(settings.MonitorPools);

        return settings;
    }

    private static Dictionary<string, string> NormalizeMonitorImages(Dictionary<string, string>? monitorImages)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (monitorId, imagePath) in monitorImages ?? [])
        {

            if (!string.IsNullOrWhiteSpace(monitorId) && !string.IsNullOrWhiteSpace(imagePath))
            {
                normalized[monitorId] = imagePath;
            }
        }

        return normalized;
    }

    private static Dictionary<string, HashSet<string>> NormalizeMonitorPools(Dictionary<string, HashSet<string>>? monitorPools)
    {
        var normalized = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (monitorId, paths) in monitorPools ?? [])
        {
            var images = (paths ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(monitorId) && images.Count > 0)
            {
                normalized[monitorId] = images;
            }
        }

        return normalized;
    }

    public void Save(Settings settings)
    {
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, path, overwrite: true);
    }
}
