using System.IO;
using System.Runtime.InteropServices;

namespace DesktopUtility.Domain;

internal sealed class WallpaperRotation(IWallpaperService wallpaper)
{
    private int nextIndex;

    public void Advance(IReadOnlyList<string> candidates, bool separateMonitors)
    {

        if (candidates.Count == 0)
        {

            return;
        }

        var monitors = separateMonitors ? wallpaper.GetMonitors() : [new MonitorTarget(null, "")];
        var failures = new List<Exception>();

        foreach (var monitor in monitors)
        {
            string path = candidates[nextIndex % candidates.Count];
            nextIndex = (nextIndex + 1) % candidates.Count;

            try
            {
                wallpaper.Apply(path, monitor.Id);
            }
            catch (Exception error) when (error is IOException or COMException)
            {
                failures.Add(error);
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateException(failures);
        }
    }
}
