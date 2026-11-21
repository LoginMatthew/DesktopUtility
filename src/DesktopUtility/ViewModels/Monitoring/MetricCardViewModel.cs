using System.ComponentModel;
using System.Globalization;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.ViewModels;

internal sealed class MetricCardViewModel(SystemMetric metric, string titleKey, MonitoringViewModel owner) : INotifyPropertyChanged
{
    private const int MaxSamples = 60;
    private readonly SampleHistory<(double? Primary, double? Secondary, double? Tertiary)> history = new(MaxSamples);
    private readonly Dictionary<string, SampleHistory<(double? Read, double? Write)>> diskHistory = new(StringComparer.OrdinalIgnoreCase);
    private MetricSample sample = MetricSample.Waiting;
    private DiskReading? selectedDisks;
    private Language Words => owner.Words;
    private CultureInfo Culture => CultureInfo.GetCultureInfo(Words.Code);

    public SystemMetric Metric => metric;
    public string Title => Words[titleKey];
    public string ToggleLabel => string.Format(Culture, Words["MetricToggle"], Title);
    public DiskFilterViewModel? DiskFilter => Metric == SystemMetric.Disk ? owner.Disks : null;
    public bool Enabled
    {
        get => owner.Includes(Metric);
        set => owner.SetMetric(Metric, value);
    }

    public string Value { get; private set; } = "";
    public string Details { get; private set; } = "";
    public double? Percentage { get; private set; }
    public bool TemperatureDriverMissing => Enabled && owner.Enabled && (sample.Reading switch
    {
        CpuReading { TemperatureCelsius: null, TemperatureStatus: TemperatureStatus.DriverMissing } => true,
        MemoryReading { TemperatureCelsius: null, TemperatureStatus: TemperatureStatus.DriverMissing } => true,
        _ => false
    });
    public MetricAlert? LoadAlert => Metric is SystemMetric.Cpu or SystemMetric.NvidiaGpu or SystemMetric.IntegratedGpu
        ? DescribeAlert(MonitoringThresholds.GetLoadAlert(Percentage), "HighLoad", "CriticalLoad")
        : null;
    public IReadOnlyList<MetricDiskVolume> Volumes { get; private set; } = [];
    public IReadOnlyList<MetricTrend> Trends { get; private set; } = [];
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(MetricSample value)
    {
        sample = value;

        if (Metric == SystemMetric.Disk && value.Status != MetricStatus.Waiting)
        {
            owner.Disks.UpdateDrives((value.Reading as DiskReading)?.Volumes ?? []);
        }

        RefreshReading();

        if (Metric == SystemMetric.Disk)
        {
            UpdateDiskHistory();
        }
        else
        {
            history.Add(sample.Reading switch
            {
                CpuReading cpu => (cpu.UsagePercent, cpu.ClockGhz, cpu.TemperatureCelsius),
                MemoryReading memory => (Percentage, memory.TemperatureCelsius, null),
                NetworkReading network => (network.DownloadBytesPerSecond, network.UploadBytesPerSecond, null),
                _ => (Percentage, null, null)
            });
        }

        RefreshTrends();
    }

    public void Reset(bool clearHistory)
    {
        sample = MetricSample.Waiting;

        if (clearHistory)
        {
            history.Clear();
            diskHistory.Clear();
        }

        Refresh();
    }

    public void Refresh()
    {
        RefreshReading();
        RefreshTrends();
    }

    public void ApplyDiskFilter()
    {
        Refresh();
        RemoveHiddenDiskHistory();
    }

    private void UpdateDiskHistory()
    {
        RemoveHiddenDiskHistory();

        foreach (var drive in selectedDisks?.Volumes ?? [])
        {

            if (!diskHistory.TryGetValue(drive.Name, out var samples))
            {
                samples = new SampleHistory<(double?, double?)>(MaxSamples);
                diskHistory.Add(drive.Name, samples);
            }

            samples.Add((drive.ReadBytesPerSecond, drive.WriteBytesPerSecond));
        }
    }

    private void RemoveHiddenDiskHistory()
    {
        var visible = (selectedDisks?.Volumes ?? []).Select(drive => drive.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string name in diskHistory.Keys.Where(name => !visible.Contains(name)).ToArray())
        {
            diskHistory.Remove(name);
        }
    }

    private void RefreshReading()
    {
        selectedDisks = Enabled && owner.Enabled && sample.Reading is DiskReading disk
            ? new DiskReading(disk.Volumes.Where(volume => owner.Disks.Includes(volume.Name)).ToArray())
            : null;
        (Value, Details, Percentage) = Describe();
        Volumes = selectedDisks?.Volumes.Select(DescribeVolume).ToArray() ?? [];
    }

    private void RefreshTrends()
    {
        var trends = new List<MetricTrend>();
        var primary = history.Select(point => point.Primary).ToArray();

        switch (Metric)
        {
            case SystemMetric.Cpu:
                AddTrend(trends, Title, primary, 100, Percent);
                AddTrend(trends, Words["ProcessorClock"], history.Select(point => point.Secondary).ToArray(), 1, Gigahertz);
                AddTrend(trends, Words["ProcessorTemperature"], history.Select(point => point.Tertiary).ToArray(), 1, Celsius);
                break;
            case SystemMetric.Disk:
                AddDiskTrends(trends);
                break;
            case SystemMetric.Memory:
                AddTrend(trends, Title, primary, 100, Percent);
                AddTrend(trends, Words["ProcessorTemperature"], history.Select(point => point.Secondary).ToArray(), 1, Celsius);
                break;
            case SystemMetric.Network:
                var upload = history.Select(point => point.Secondary).ToArray();
                double scale = Math.Max(125_000, primary.Concat(upload).Max() ?? 0);
                AddTrend(trends, Words["Download"], primary, scale, NetworkRate);
                AddTrend(trends, Words["Upload"], upload, scale, NetworkRate);
                break;
            default:
                AddTrend(trends, Title, primary, 100, Percent);
                break;
        }

        Trends = trends;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    private void AddDiskTrends(List<MetricTrend> trends)
    {

        foreach (var drive in selectedDisks?.Volumes ?? [])
        {
            var samples = diskHistory.GetValueOrDefault(drive.Name);
            var reads = samples?.Select(sample => sample.Read).ToArray() ?? [];
            var writes = samples?.Select(sample => sample.Write).ToArray() ?? [];
            double? peak = reads.Concat(writes).Max();
            double maximum = Math.Max(1024, peak ?? 0);
            trends.Add(new MetricTrend(drive.DisplayName, reads, maximum,
                peak.HasValue ? $"0–{Bytes(maximum)}/s" : Words["MetricUnavailable"],
                writes, Words["DiskReadLegend"], Words["DiskWriteLegend"]));
        }
    }

    private static void AddTrend(List<MetricTrend> trends, string label, double?[] samples, double minimum, Func<double, string> format)
    {

        if (samples.Max() is { } peak)
        {
            double maximum = Math.Max(minimum, peak);
            trends.Add(new MetricTrend(label, samples, maximum, $"0–{format(maximum)}"));
        }
    }

    private (string Value, string Details, double? Percentage) Describe()
    {

        if (!Enabled || !owner.Enabled)
        {

            return (Words["MetricOff"], "", null);
        }

        return sample.Reading switch
        {
            CpuReading cpu => DescribeCpu(cpu),
            MemoryReading memory => DescribeMemory(memory),
            DiskReading when selectedDisks is { } disk => DescribeDisk(disk),
            NvidiaReading nvidia => DescribeNvidia(nvidia),
            NetworkReading network => DescribeNetwork(network),
            IntegratedGpuReading gpu => DescribeGpu(gpu.Devices.Select(device => (device.Name, device.UsagePercent))),
            _ => (Words[sample.Status == MetricStatus.Unavailable ? "MetricUnavailable" : "MetricWaiting"], "", null)
        };
    }

    private (string, string, double?) DescribeCpu(CpuReading cpu)
    {
        string clock = cpu.ClockGhz is { } ghz ? Gigahertz(ghz) : Words["MetricUnavailable"];
        string heat = DescribeTemperature(cpu.TemperatureCelsius, cpu.TemperatureStatus);
        string details = string.Join(Environment.NewLine,
            string.Format(Culture, Words["CpuClock"], clock),
            string.Format(Culture, Words["CpuTemperature"], heat));

        return (Percent(cpu.UsagePercent), details, cpu.UsagePercent);
    }

    private (string, string, double?) DescribeMemory(MemoryReading memory)
    {
        double usage = 100 * memory.UsedBytes / (double)memory.TotalBytes;
        string capacity = string.Format(Culture, Words["MemoryDetails"], Bytes(memory.UsedBytes), Bytes(memory.TotalBytes));
        string temperature = string.Format(Culture, Words["MemoryTemperature"],
            DescribeTemperature(memory.TemperatureCelsius, memory.TemperatureStatus));

        return (Percent(usage), string.Join(Environment.NewLine, capacity, temperature), usage);
    }

    private string DescribeTemperature(double? temperature, TemperatureStatus status) =>
        temperature is { } celsius ? Celsius(celsius) : Words[status switch
        {
            TemperatureStatus.Waiting => "MetricWaiting",
            TemperatureStatus.DriverMissing => "TemperatureDriverMissing",
            TemperatureStatus.AccessDenied => "TemperatureAccessDenied",
            _ => "TemperatureSensorUnavailable"
        }];

    private (string, string, double?) DescribeDisk(DiskReading disk)
    {
        string rate = disk.Volumes.Count == 0
            ? Words["NoMonitoredDisks"]
            : DiskRates(disk.ReadBytesPerSecond, disk.WriteBytesPerSecond);

        return (rate, "", null);
    }

    private MetricDiskVolume DescribeVolume(DiskVolumeReading volume)
    {
        string freePercent = volume.TotalBytes > 0 && volume.FreeBytes >= 0 && volume.FreeBytes <= volume.TotalBytes
            ? Percent(100d * volume.FreeBytes / volume.TotalBytes)
            : "—";
        string capacity = string.Format(Culture, Words["DiskCapacity"],
            volume.DisplayName, Bytes(volume.FreeBytes), Bytes(volume.TotalBytes), freePercent);
        var alert = DescribeAlert(MonitoringThresholds.GetFreeSpaceAlert(volume.FreeBytes, volume.TotalBytes),
            "LowDiskSpace", "CriticalDiskSpace");

        return new MetricDiskVolume(capacity, DiskRates(volume.ReadBytesPerSecond, volume.WriteBytesPerSecond), alert);
    }

    private string DiskRates(double? read, double? write) => string.Format(Culture, Words["DiskRates"], Rate(read), Rate(write));

    private MetricAlert? DescribeAlert(MonitoringAlertLevel level, string warningKey, string criticalKey) => level switch
    {
        MonitoringAlertLevel.Warning => new MetricAlert(Words[warningKey], false),
        MonitoringAlertLevel.Critical => new MetricAlert(Words[criticalKey], true),
        _ => null
    };

    private (string, string, double?) DescribeNetwork(NetworkReading network)
    {
        var lines = new List<string> { $"{Words["Upload"]} {NetworkRate(network.UploadBytesPerSecond)}" };
        lines.AddRange((network.Adapters ?? []).Select(adapter =>
            $"{adapter.Name} · ↓ {NetworkRate(adapter.DownloadBytesPerSecond)} · ↑ {NetworkRate(adapter.UploadBytesPerSecond)}"));

        return ($"{Words["Download"]} {NetworkRate(network.DownloadBytesPerSecond)}", string.Join(Environment.NewLine, lines), null);
    }

    private (string, string, double?) DescribeNvidia(NvidiaReading nvidia) =>
        DescribeGpu(nvidia.Devices.Select(device => (DescribeNvidiaDevice(device), (double?)device.UsagePercent)));

    private string DescribeNvidiaDevice(NvidiaDeviceReading device)
    {
        var details = new List<string> { device.Name };

        if (device.TemperatureCelsius is { } temperature)
        {
            details.Add(Celsius(temperature));
        }

        if (device.UsedMemoryBytes is { } used && device.TotalMemoryBytes is { } total)
        {
            details.Add($"VRAM {Bytes(used)} / {Bytes(total)}");
        }

        return string.Join(" · ", details);
    }

    private (string, string, double?) DescribeGpu(IEnumerable<(string Name, double? UsagePercent)> devices)
    {
        var list = devices.ToArray();
        var usage = list.Max(device => device.UsagePercent);
        string details = string.Join(Environment.NewLine, list.Where(device => !string.IsNullOrWhiteSpace(device.Name)).Select(device =>
            $"{device.Name} · {(device.UsagePercent is { } percent ? Percent(percent) : Words["MetricUnavailable"])}"));

        return (usage is { } value ? Percent(value) : Words["MetricUnavailable"], details, usage);
    }

    private string Percent(double value) => $"{value.ToString("0.#", Culture)}%";
    private string Gigahertz(double value) => $"{value.ToString("0.00", Culture)} GHz";
    private string Celsius(double value) => $"{value.ToString("0.#", Culture)} °C";
    private string NetworkRate(double bytes) => $"{(bytes * 8 / 1_000_000).ToString("0.00", Culture)} Mbit/s";

    private string Bytes(double value)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        int index = 0;

        while (value >= 1024 && index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return $"{value.ToString("0.#", Culture)} {units[index]}";
    }

    private string Rate(double? value) => value is { } bytes ? $"{Bytes(bytes)}/s" : "—";
}
