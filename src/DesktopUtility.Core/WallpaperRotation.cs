namespace DesktopUtility.Core;

internal sealed class WallpaperRotation(IWallpaperService wallpaper)
{
    private int nextIndex;

    public async Task<WallpaperActionResult> AdvanceAsync(IReadOnlyList<string> candidates, bool separateMonitors)
    {

        if (candidates.Count == 0)
        {

            return WallpaperActionResult.Skipped;
        }

        IReadOnlyList<MonitorTarget> monitors = separateMonitors
            ? await wallpaper.GetMonitorsAsync().ConfigureAwait(false)
            : [new MonitorTarget(null, "")];

        bool failed = false;

        foreach (var monitor in monitors)
        {
            string path = candidates[nextIndex % candidates.Count];
            nextIndex = (nextIndex + 1) % candidates.Count;

            try
            {
                await wallpaper.ApplyAsync(path, monitor.Id).ConfigureAwait(false);
            }
            catch (WallpaperOperationException)
            {
                failed = true;
            }
        }

        return failed ? WallpaperActionResult.Failed : WallpaperActionResult.Applied;
    }
}
