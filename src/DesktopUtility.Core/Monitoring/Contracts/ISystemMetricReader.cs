namespace DesktopUtility.Core.Monitoring;

public interface ISystemMetricReader : IDisposable
{
    MetricSample Read();
}
