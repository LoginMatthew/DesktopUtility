namespace DesktopUtility.Core.Monitoring;

public sealed record TemperatureReading(TemperatureStatus Status, double? Celsius = null)
{
    public static TemperatureReading Unavailable { get; } = new(TemperatureStatus.Unavailable);
}
