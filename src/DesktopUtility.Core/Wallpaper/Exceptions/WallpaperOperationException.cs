namespace DesktopUtility.Core;

public class WallpaperOperationException(Exception cause) : Exception(cause.Message, cause);
