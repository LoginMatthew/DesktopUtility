namespace DesktopUtility.Core.Monitoring;

[Flags]
public enum SystemMetric
{
    None = 0,
    Cpu = 1,
    Memory = 2,
    Disk = 4,
    NvidiaGpu = 8,
    Network = 16,
    IntegratedGpu = 32,
    All = Cpu | Memory | Disk | NvidiaGpu | Network | IntegratedGpu
}
