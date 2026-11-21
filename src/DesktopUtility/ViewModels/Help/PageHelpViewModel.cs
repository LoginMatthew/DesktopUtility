namespace DesktopUtility.ViewModels;

internal sealed class PageHelpViewModel(Language language, HelpTopic topic)
{
    public string Title => language[topic switch
    {
        HelpTopic.Gallery => "GalleryHelpTitle",
        HelpTopic.Settings => "SettingsHelpTitle",
        HelpTopic.Monitoring => "MonitoringHelpTitle",
        _ => throw new ArgumentOutOfRangeException(nameof(topic))
    }];
    public string CloseLabel => language["CloseHelp"];
    public IReadOnlyList<string> Steps => topic switch
    {
        HelpTopic.Gallery => GallerySteps(language),
        HelpTopic.Settings => SettingsSteps(language),
        HelpTopic.Monitoring => MonitoringSteps(language),
        _ => throw new ArgumentOutOfRangeException(nameof(topic))
    };

    internal static IReadOnlyList<string> GallerySteps(Language language) =>
    [
        language["UsageFolder"],
        language["UsageApply"],
        language["UsageMonitorSelect"],
        language["UsageMonitorPool"],
        language["UsageMonitorRandom"]
    ];

    internal static IReadOnlyList<string> SettingsSteps(Language language) =>
    [
        language["UsageRotation"],
        language["UsageAppearance"],
        language["UsageStartup"]
    ];

    private static IReadOnlyList<string> MonitoringSteps(Language language) =>
    [
        language["UsageMonitoringSwitches"],
        language["UsageMonitoringDiskAlerts"],
        language["UsageMonitoringLoadAlerts"],
        language["UsageMonitoringNetwork"],
        language["UsageMonitoringTemperature"],
        language["UsageMonitoringFps"]
    ];
}
