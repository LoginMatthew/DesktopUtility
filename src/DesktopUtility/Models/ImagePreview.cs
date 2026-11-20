using System.Windows.Media.Imaging;

namespace DesktopUtility.Models;

internal sealed record ImagePreview(BitmapSource? Thumbnail, int PixelWidth, int PixelHeight, long? FileSizeBytes)
{
    public bool HasDimensions => PixelWidth > 0 && PixelHeight > 0;
}
