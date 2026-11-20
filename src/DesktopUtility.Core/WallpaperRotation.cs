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

        var assignments = new List<MonitorWallpaperAssignment>(monitors.Count);

        foreach (var monitor in monitors)
        {
            string path = candidates[nextIndex % candidates.Count];
            nextIndex = (nextIndex + 1) % candidates.Count;
            assignments.Add(new MonitorWallpaperAssignment(monitor.Id, path));
        }

        return await ApplyAssignmentsAsync(assignments).ConfigureAwait(false);
    }

    public async Task<WallpaperActionResult> ApplyAssignmentsAsync(IReadOnlyList<MonitorWallpaperAssignment> assignments)
    {
        bool failed = false;

        foreach (var assignment in assignments)
        {
            try
            {
                await wallpaper.ApplyAsync(assignment.ImagePath, assignment.MonitorId).ConfigureAwait(false);
            }
            catch (WallpaperOperationException)
            {
                failed = true;
            }
        }

        return failed ? WallpaperActionResult.Failed : WallpaperActionResult.Applied;
    }
}
