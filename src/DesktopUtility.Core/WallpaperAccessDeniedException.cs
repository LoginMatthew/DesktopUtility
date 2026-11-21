namespace DesktopUtility.Core;

public sealed class WallpaperAccessDeniedException(Exception cause) : WallpaperOperationException(cause);
