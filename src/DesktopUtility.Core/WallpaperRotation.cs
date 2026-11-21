namespace DesktopUtility.Core;

internal sealed class WallpaperRotation(IWallpaperService wallpaper)
{
    private readonly Dictionary<string, string> lastImageByMonitor = new(StringComparer.OrdinalIgnoreCase);
    private int nextIndex;

    public async Task<WallpaperActionResult> AdvanceAsync(
        IReadOnlyList<string> candidates,
        bool separateMonitors,
        IReadOnlyDictionary<string, IReadOnlyList<string>> monitorPools)
    {
        IReadOnlyList<MonitorTarget> monitors = separateMonitors
            ? await wallpaper.GetMonitorsAsync().ConfigureAwait(false)
            : [new MonitorTarget(null, "")];

        var unavailableImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool attempted = false;
        bool failed = monitors.Count == 0;

        foreach (var monitor in monitors)
        {
            var pool = monitor.Id is { } id ? monitorPools.GetValueOrDefault(id) : null;
            var images = pool ?? candidates;

            if (images.Count == 0)
            {
                continue;
            }

            attempted = true;
            bool applied = await TryApplyNextAsync(images, monitor.Id, unavailableImages, usedImages, randomize: pool is not null)
                .ConfigureAwait(false);
            failed |= !applied;
        }

        return failed ? WallpaperActionResult.Failed
            : attempted ? WallpaperActionResult.Applied
            : WallpaperActionResult.Skipped;
    }

    public async Task<WallpaperActionResult> ApplyRandomAsync(IReadOnlyList<string> pool, string monitorId)
    {
        var unavailableImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool applied = await TryApplyNextAsync(pool, monitorId, unavailableImages, usedImages, randomize: true).ConfigureAwait(false);

        return applied ? WallpaperActionResult.Applied : WallpaperActionResult.Failed;
    }

    public async Task<WallpaperActionResult> ApplyAssignmentsAsync(IReadOnlyList<MonitorWallpaperAssignment> assignments)
    {
        bool failed = false;

        foreach (var assignment in assignments)
        {
            try
            {
                await wallpaper.ApplyAsync(assignment.ImagePath, assignment.MonitorId).ConfigureAwait(false);
                RememberImage(assignment.ImagePath, assignment.MonitorId);
            }
            catch (WallpaperOperationException error) when (error is not WallpaperAccessDeniedException)
            {
                failed = true;
            }
        }

        return failed ? WallpaperActionResult.Failed : WallpaperActionResult.Applied;
    }

    public async Task<WallpaperActionResult> ApplyAsync(string path, string? monitorId)
    {
        await wallpaper.ApplyAsync(path, monitorId).ConfigureAwait(false);
        RememberImage(path, monitorId);

        return WallpaperActionResult.Applied;
    }

    private async Task<bool> TryApplyNextAsync(
        IReadOnlyList<string> candidates,
        string? monitorId,
        HashSet<string> unavailableImages,
        HashSet<string> usedImages,
        bool randomize)
    {
        var ordered = randomize ? RandomOrder(candidates, monitorId) : candidates;
        int index = FindNextImage(ordered, randomize ? 0 : nextIndex, unavailableImages, usedImages);

        while (index >= 0)
        {
            string path = ordered[index];

            if (!randomize)
            {
                nextIndex = (index + 1) % ordered.Count;
            }

            try
            {
                await ApplyAsync(path, monitorId).ConfigureAwait(false);
                usedImages.Add(path);

                return true;
            }
            catch (WallpaperImageException)
            {
                unavailableImages.Add(path);
                index = FindNextImage(ordered, randomize ? 0 : nextIndex, unavailableImages, usedImages);
            }
            catch (WallpaperOperationException error) when (error is not WallpaperAccessDeniedException)
            {

                return false;
            }
        }

        return false;
    }

    private IReadOnlyList<string> RandomOrder(IReadOnlyList<string> candidates, string? monitorId)
    {
        var shuffled = candidates.ToArray();
        Random.Shared.Shuffle(shuffled);
        string? last = monitorId is null ? null : lastImageByMonitor.GetValueOrDefault(monitorId);

        return last is null ? shuffled : shuffled.OrderBy(path => string.Equals(path, last, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private void RememberImage(string path, string? monitorId)
    {

        if (monitorId is not null)
        {
            lastImageByMonitor[monitorId] = path;
        }
    }

    private static int FindNextImage(
        IReadOnlyList<string> candidates,
        int startIndex,
        HashSet<string> unavailableImages,
        HashSet<string> usedImages)
    {
        int reusableIndex = -1;

        for (int offset = 0; offset < candidates.Count; offset++)
        {
            int index = (startIndex + offset) % candidates.Count;
            string path = candidates[index];

            if (unavailableImages.Contains(path))
            {
                continue;
            }

            if (!usedImages.Contains(path))
            {

                return index;
            }

            if (reusableIndex < 0)
            {
                reusableIndex = index;
            }
        }

        return reusableIndex;
    }
}
