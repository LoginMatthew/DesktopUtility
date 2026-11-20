namespace DesktopUtility.ViewModels;

internal sealed class AboutViewModel(Language language)
{
    public Language Words => language;
    public string Author => "LoginMatthew";
    public string Version => typeof(AboutViewModel).Assembly.GetName().Version?.ToString(3) ?? "—";

    public IReadOnlyList<string> UsageSteps =>
    [
        language["UsageFolder"],
        language["UsageApply"],
        language["UsageRotation"],
        language["UsageTray"]
    ];
}
