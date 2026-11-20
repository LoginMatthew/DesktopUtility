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

        return settings;
    }

    public void Save(Settings settings)
    {
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, path, overwrite: true);
    }
}

