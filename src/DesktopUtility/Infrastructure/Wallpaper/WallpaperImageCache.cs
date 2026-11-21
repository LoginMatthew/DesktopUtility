using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopUtility.Infrastructure.Wallpaper;

internal static class WallpaperImageCache
{
    public static string GetCompatibleImage(string sourcePath)
    {
        var source = new FileInfo(sourcePath);
        string identity = string.Create(CultureInfo.InvariantCulture, $"{source.FullName}|{source.Length}|{source.LastWriteTimeUtc.Ticks}");
        string cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        string folder = Path.Combine(Path.GetTempPath(), "DesktopUtility", "Wallpapers");
        Directory.CreateDirectory(folder);
        string compatiblePath = Path.Combine(folder, cacheKey + ".jpg");

        if (File.Exists(compatiblePath))
        {

            return compatiblePath;
        }

        using var input = File.OpenRead(sourcePath);
        var image = BitmapDecoder.Create(input, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames.FirstOrDefault()
            ?? throw new InvalidDataException("The wallpaper file does not contain an image.");
        var rgb = new FormatConvertedBitmap(image, PixelFormats.Bgr24, null, 0);
        var encoder = new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(rgb));
        string temporaryPath = compatiblePath + ".tmp";
        try
        {
            using (var output = File.Create(temporaryPath))
            {
                encoder.Save(output);
            }

            File.Move(temporaryPath, compatiblePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }

        return compatiblePath;
    }
}
