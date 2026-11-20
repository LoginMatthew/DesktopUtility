using System.Globalization;
using DesktopUtility.Infrastructure.Images;

namespace DesktopUtility.ViewModels;

internal sealed class GalleryViewModel(WallpaperSession session, Action<string> loadFolder)
{
    private const int PageSize = 36;
    public string Folder => session.Settings.Folder;
    public bool HasImages => session.Images.Count > 0;
    public int PageIndex { get; private set; }
    public int LastPage => Math.Max(0, (session.Images.Count - 1) / PageSize);
    public IEnumerable<string> PageImages => session.Images.Skip(PageIndex * PageSize).Take(PageSize);

    public bool SelectedOnly
    {
        get => session.Settings.SelectedOnly;
        set
        {
            session.Settings.SelectedOnly = value;
            session.RefreshCandidates();
            session.Save();
        }
    }

    public void ChangePage(int offset) => PageIndex = Math.Clamp(PageIndex + offset, 0, LastPage);
    public void ResetPage() => PageIndex = 0;
    public void OpenFolder(string path) => loadFolder(path);
    public void Refresh() => loadFolder(Folder);
    public bool Includes(string path) => session.Settings.Selected.Contains(path);

    public void Include(string path, bool enabled)
    {

        if (enabled)
        {
            session.Settings.Selected.Add(path);
        }
        else
        {
            session.Settings.Selected.Remove(path);
        }

        if (SelectedOnly)
        {
            session.RefreshCandidates();
        }

        session.Save();
    }

    public void SetFavorite(string? path)
    {
        session.Settings.Favorite = path;
        session.Save();
    }

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
