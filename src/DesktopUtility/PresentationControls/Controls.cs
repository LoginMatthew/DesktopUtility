using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopUtility.PresentationControls;

internal static class Controls
{
    public static Button Button(string label, Action action)
    {
        var button = new Button { Content = label };
        button.Click += (_, _) => action();

        return button;
    }

    public static CheckBox CheckBox(string label, bool value, Action<bool> change)
    {
        var checkBox = new CheckBox { Content = label, IsChecked = value };
        checkBox.Click += (_, _) => change(checkBox.IsChecked == true);

        return checkBox;
    }

    public static TextBlock Label(string text)
    {

        return new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 8) };
    }

    public static SolidColorBrush Hue(double hue, double saturation, double lightness)
    {
        hue = Math.Clamp(hue, 0, 359) / 60;
        double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double intermediate = chroma * (1 - Math.Abs(hue % 2 - 1));
        double offset = lightness - chroma / 2;
        var (red, green, blue) = hue switch
        {
            < 1 => (chroma, intermediate, 0d),
            < 2 => (intermediate, chroma, 0d),
            < 3 => (0d, chroma, intermediate),
            < 4 => (0d, intermediate, chroma),
            < 5 => (intermediate, 0d, chroma),
            _ => (chroma, 0d, intermediate)
        };

        return new SolidColorBrush(Color.FromRgb(
            (byte)((red + offset) * 255), (byte)((green + offset) * 255), (byte)((blue + offset) * 255)));
    }
}
