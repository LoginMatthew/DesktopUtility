namespace DesktopUtility.Core;

public sealed record MonitorTarget(string? Id, string Name, int? Number = null)
{
    public override string ToString() => Name;
}
