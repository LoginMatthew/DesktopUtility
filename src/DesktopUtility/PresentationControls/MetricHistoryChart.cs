using System.Windows;
using System.Windows.Media;

namespace DesktopUtility.PresentationControls;

public sealed class MetricHistoryChart : FrameworkElement
{
    public static readonly DependencyProperty SamplesProperty = DependencyProperty.Register(
        nameof(Samples), typeof(IReadOnlyList<double?>), typeof(MetricHistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(MetricHistoryChart),
        new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SecondarySamplesProperty = DependencyProperty.Register(
        nameof(SecondarySamples), typeof(IReadOnlyList<double?>), typeof(MetricHistoryChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SecondaryBrushProperty = DependencyProperty.Register(
        nameof(SecondaryBrush), typeof(Brush), typeof(MetricHistoryChart),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush), typeof(Brush), typeof(MetricHistoryChart),
        new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
        nameof(GridBrush), typeof(Brush), typeof(MetricHistoryChart),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<double?>? Samples
    {
        get => (IReadOnlyList<double?>?)GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public IReadOnlyList<double?>? SecondarySamples
    {
        get => (IReadOnlyList<double?>?)GetValue(SecondarySamplesProperty);
        set => SetValue(SecondarySamplesProperty, value);
    }

    public Brush SecondaryBrush
    {
        get => (Brush)GetValue(SecondaryBrushProperty);
        set => SetValue(SecondaryBrushProperty, value);
    }

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public Brush GridBrush
    {
        get => (Brush)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        double width = Math.Max(0, ActualWidth - 4);
        double height = Math.Max(0, ActualHeight - 4);
        var grid = new Pen(GridBrush, 0.5);

        for (int index = 0; index < 5; index++)
        {
            double y = 2 + height * index / 4;
            drawing.DrawLine(grid, new Point(2, y), new Point(2 + width, y));
        }

        if (Maximum <= 0)
        {

            return;
        }

        DrawSeries(drawing, Samples ?? [], new Pen(LineBrush, 1.75), width, height);
        DrawSeries(drawing, SecondarySamples ?? [], new Pen(SecondaryBrush, 1.4) { DashStyle = DashStyles.Dash }, width, height);
    }

    private void DrawSeries(DrawingContext drawing, IReadOnlyList<double?> samples, Pen line, double width, double height)
    {
        Point? previous = null;

        for (int index = 0; index < samples.Count; index++)
        {

            if (samples[index] is not { } value)
            {
                previous = null;
                continue;
            }

            double x = 2 + width * (samples.Count == 1 ? 1 : index / (samples.Count - 1d));
            double y = 2 + height * (1 - Math.Clamp(value / Maximum, 0, 1));
            var point = new Point(x, y);

            if (previous is { } from)
            {
                drawing.DrawLine(line, from, point);
            }
            else
            {
                drawing.DrawEllipse(line.Brush, null, point, 1.6, 1.6);
            }

            previous = point;
        }
    }
}
