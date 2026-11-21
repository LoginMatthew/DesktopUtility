namespace DesktopUtility.Infrastructure.Monitoring.Fps;

internal sealed record WindowCaptureTarget(nint Handle, int ProcessId, string Title);
