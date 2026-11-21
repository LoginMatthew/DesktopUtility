namespace DesktopUtility.ViewModels;

internal enum HelpTopic
{
    Gallery,
    Settings
}

internal sealed class PageHelpViewModel(Language language, HelpTopic topic)
{
    public string Title => language[topic == HelpTopic.Gallery ? "GalleryHelpTitle" : "SettingsHelpTitle"];
    public string CloseLabel => language["CloseHelp"];
    public IReadOnlyList<string> Steps => topic == HelpTopic.Gallery ? GallerySteps(language) : SettingsSteps(language);

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
}
