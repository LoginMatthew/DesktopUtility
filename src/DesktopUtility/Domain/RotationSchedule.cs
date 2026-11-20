namespace DesktopUtility.Domain;

internal sealed class RotationSchedule(TimeProvider clock)
{
    private long? startedAt;
    private TimeSpan interval;

    public TimeSpan? Remaining => startedAt is { } start ? interval - clock.GetElapsedTime(start) : null;

    public bool IsDue => Remaining is { } remaining && remaining <= TimeSpan.Zero;

    public void Restart(bool enabled, int candidateCount, int intervalMinutes)
    {
        interval = TimeSpan.FromMinutes(intervalMinutes);
        startedAt = enabled && candidateCount > 0 ? clock.GetTimestamp() : null;
    }
}
