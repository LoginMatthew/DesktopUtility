namespace DesktopUtility.Infrastructure.Monitoring.Fps;

internal sealed record MonitorWindows(string Id, string Name, IReadOnlyList<WindowCaptureTarget> Windows);
