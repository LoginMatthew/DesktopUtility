namespace DesktopUtility.Core;

public interface IWallpaperService
{
    Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync();
    Task ApplyAsync(string filePath, string? monitorId);
}
