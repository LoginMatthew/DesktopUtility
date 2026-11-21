namespace DesktopUtility.ViewModels;

internal sealed record MetricTrend(string Label, IReadOnlyList<double?> Samples, double Maximum, string Scale,
    IReadOnlyList<double?>? SecondarySamples = null, string? PrimaryLabel = null, string? SecondaryLabel = null);
