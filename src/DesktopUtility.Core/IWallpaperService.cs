namespace DesktopUtility.Core;

public sealed record MonitorTarget(string? Id, string Name)
{
    public override string ToString() => Name;
}

public interface IWallpaperService
{
    Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync();
    Task ApplyAsync(string filePath, string? monitorId);
}

public sealed class WallpaperOperationException(Exception cause) : Exception(cause.Message, cause);
