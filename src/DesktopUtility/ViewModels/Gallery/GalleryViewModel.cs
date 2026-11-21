using System.Globalization;
using DesktopUtility.Infrastructure.Images;

namespace DesktopUtility.ViewModels;

internal sealed class GalleryViewModel(WallpaperSession session, Action<string> loadFolder)
{
    private const int PageSize = 36;
    public string Folder => session.Settings.Folder;
    public bool HasImages => session.Images.Count > 0 || session.Settings.MonitorPools.Count > 0;
    public int PageIndex { get; private set; }
    public int LastPage => Math.Max(0, (FilteredImages.Count - 1) / PageSize);
    public IEnumerable<string> PageImages => FilteredImages.Skip(PageIndex * PageSize).Take(PageSize);
    public string? TargetMonitorId { get; private set; }
    public GalleryMonitorFilter Filter { get; private set; }

    private IReadOnlyList<string> FilteredImages
    {
        get
        {
            var pool = TargetMonitorId is { } monitorId ? session.Settings.MonitorPools.GetValueOrDefault(monitorId) : null;

            return Filter switch
            {
                GalleryMonitorFilter.InPool => pool?.Order(StringComparer.OrdinalIgnoreCase).ToArray() ?? [],
                GalleryMonitorFilter.OutsidePool when pool is not null => session.Images.Where(path => !pool.Contains(path)).ToArray(),
                _ => session.Images
            };
        }
    }

    public bool SeparateMonitors
    {
        get => session.Settings.SeparateMonitors;
        set => session.SetSeparateMonitors(value);
    }

    public bool SelectedOnly
    {
        get => session.Settings.SelectedOnly;
        set => session.SetSelectedOnly(value);
    }

    public void ChangePage(int offset) => PageIndex = Math.Clamp(PageIndex + offset, 0, LastPage);
    public void ResetPage() => PageIndex = 0;
    public void OpenFolder(string path) => loadFolder(path);
    public void Refresh() => loadFolder(Folder);
    public bool Includes(string path) => session.Settings.Selected.Contains(path);

    public void SelectMonitor(string? monitorId)
    {
        TargetMonitorId = monitorId;

        if (monitorId is null)
        {
            Filter = GalleryMonitorFilter.AllImages;
        }

        ResetPage();
    }

    public void SetMonitorFilter(GalleryMonitorFilter filter)
    {
        Filter = filter;
        ResetPage();
    }

    public bool IsInMonitorPool(string monitorId, string path) =>
        session.Settings.MonitorPools.GetValueOrDefault(monitorId)?.Contains(path) == true;

    public int GetMonitorPoolCount(string monitorId) => session.Settings.MonitorPools.GetValueOrDefault(monitorId)?.Count ?? 0;

    public void ChangeMonitorPool(string monitorId, IEnumerable<string> paths, bool include)
    {
        session.ChangeMonitorPool(monitorId, paths, include);
        PageIndex = Math.Min(PageIndex, LastPage);
    }

    public void ApplyRandomFromPool(string monitorId) => session.ApplyRandomFromPool(monitorId);

    public void SetAllSelected(bool selected) => session.SetAllSelected(selected);

    public string? GetWallpaperAssignment(string monitorId) => session.Settings.MonitorAssignments.GetValueOrDefault(monitorId);
    public void AssignWallpaper(string monitorId, string path) => session.SetMonitorAssignment(monitorId, path);
    public void ClearWallpaperAssignment(string monitorId) => session.ClearMonitorAssignment(monitorId);

    public string? GetMonitorFavorite(string monitorId) => session.Settings.MonitorFavorites.GetValueOrDefault(monitorId);
    public void SetMonitorFavorite(string monitorId, string path) => session.SetMonitorFavorite(monitorId, path);
    public void ClearMonitorFavorite(string monitorId) => session.ClearMonitorFavorite(monitorId);

    public void ApplyMonitorAssignments(IEnumerable<string> connectedMonitorIds)
    {
        var assignments = new List<MonitorWallpaperAssignment>();

        foreach (string monitorId in connectedMonitorIds)
        {

            if (GetWallpaperAssignment(monitorId) is { } path)
            {
                assignments.Add(new MonitorWallpaperAssignment(monitorId, path));
            }
        }

        session.ApplyMonitorAssignments(assignments);
    }

    public void Include(string path, bool enabled) => session.SetSelected(path, enabled);
    public string? FavoritePath => session.Settings.Favorite;
    public bool IsFavorite(string path) => string.Equals(path, FavoritePath, StringComparison.OrdinalIgnoreCase);
    public void SetFavorite(string? path) => session.SetFavorite(path);

    public void ApplyFavorite() => session.ApplyFavorite();
    public void Apply(string? path, string? monitorId)
    {

        if (path is not null && monitorId is not null)
        {
            AssignWallpaper(monitorId, path);
        }

        session.Apply(path, monitorId);
    }
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
