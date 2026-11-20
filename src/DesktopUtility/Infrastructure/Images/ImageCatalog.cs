using System.IO;
using System.Windows.Media.Imaging;

namespace DesktopUtility.Infrastructure.Images;

internal static class ImageCatalog
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp" };

    public static Task<string[]> ReadAsync(string folder, CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(folder))
        {

            return Task.FromResult(Array.Empty<string>());
        }

        return Task.Run(() => Directory.EnumerateFiles(folder)
            .Where(path =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Extensions.Contains(Path.GetExtension(path));
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray(), cancellationToken);
    }

    public static ImagePreview ReadPreview(string path)
    {
        long? fileSize = null;
        int width = 0;
        int height = 0;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            fileSize = stream.Length;
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
            var frame = decoder.Frames.FirstOrDefault();

            if (frame is null)
            {

                return new ImagePreview(null, 0, 0, fileSize);
            }

            width = frame.PixelWidth;
            height = frame.PixelHeight;

            stream.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();

            if (width >= height)
            {
                image.DecodePixelWidth = Math.Min(width, 280);
            }
            else
            {
                image.DecodePixelHeight = Math.Min(height, 280);
            }

            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            return new ImagePreview(image, width, height, fileSize);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or FormatException or System.Runtime.InteropServices.COMException)
        {

            return new ImagePreview(null, width, height, fileSize);
        }
    }
}
