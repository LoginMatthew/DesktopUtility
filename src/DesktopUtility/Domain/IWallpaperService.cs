namespace DesktopUtility.Domain;

internal sealed record MonitorTarget(string? Id, string Name)
{
    public override string ToString() => Name;
}

internal interface IWallpaperService
{
    IReadOnlyList<MonitorTarget> GetMonitors();
    void Apply(string filePath, string? monitorId);
}

