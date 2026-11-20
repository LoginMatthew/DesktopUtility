using System.Globalization;
using DesktopUtility.Infrastructure.Images;

namespace DesktopUtility.ViewModels;

internal sealed class GalleryViewModel(WallpaperSession session, Action<string> loadFolder)
{
    private const int PageSize = 36;
    private readonly Dictionary<string, string> monitorAssignments = new(StringComparer.OrdinalIgnoreCase);
    public string Folder => session.Settings.Folder;
    public bool HasImages => session.Images.Count > 0;
    public int PageIndex { get; private set; }
    public int LastPage => Math.Max(0, (session.Images.Count - 1) / PageSize);
    public IEnumerable<string> PageImages => session.Images.Skip(PageIndex * PageSize).Take(PageSize);

    public bool SelectedOnly
    {
        get => session.Settings.SelectedOnly;
        set => session.SetSelectedOnly(value);
    }

    public void ChangePage(int offset) => PageIndex = Math.Clamp(PageIndex + offset, 0, LastPage);
    public void ResetPage()
    {
        PageIndex = 0;
        var currentImages = session.Images.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingAssignments = monitorAssignments.Where(item => !currentImages.Contains(item.Value)).Select(item => item.Key).ToArray();

        foreach (string monitorId in missingAssignments)
        {
            monitorAssignments.Remove(monitorId);
        }
    }
    public void OpenFolder(string path) => loadFolder(path);
    public void Refresh() => loadFolder(Folder);
    public bool Includes(string path) => session.Settings.Selected.Contains(path);

    public void SetAllSelected(bool selected) => session.SetAllSelected(selected);

    public string? GetWallpaperAssignment(string monitorId) => monitorAssignments.GetValueOrDefault(monitorId);
    public void AssignWallpaper(string monitorId, string path) => monitorAssignments[monitorId] = path;
    public void ClearWallpaperAssignment(string monitorId) => monitorAssignments.Remove(monitorId);

    public void ApplyMonitorAssignments(IEnumerable<string> connectedMonitorIds)
    {
        var assignments = new List<MonitorWallpaperAssignment>();

        foreach (string monitorId in connectedMonitorIds)
        {

            if (monitorAssignments.TryGetValue(monitorId, out string? path))
            {
                assignments.Add(new MonitorWallpaperAssignment(monitorId, path));
            }
        }

        session.ApplyMonitorAssignments(assignments);
    }

    public void Include(string path, bool enabled) => session.SetSelected(path, enabled);
    public void SetFavorite(string? path) => session.SetFavorite(path);

    public void ApplyFavorite() => session.ApplyFavorite();
    public void Apply(string? path, string? monitorId) => session.Apply(path, monitorId);
    public Task<ImagePreview> ReadPreviewAsync(string path, CancellationToken cancellationToken) =>
        Task.Run(() => ImageCatalog.ReadPreview(path), cancellationToken);

    public static string FormatFileSize(long bytes, CultureInfo culture)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return string.Concat(size.ToString(unit == 0 ? "N0" : "0.#", culture), " ", units[unit]);
    }

    public Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync() => session.GetMonitorsAsync();
}
