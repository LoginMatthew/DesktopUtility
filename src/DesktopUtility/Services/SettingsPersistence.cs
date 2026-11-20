using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using DesktopUtility.Infrastructure.Persistence;

namespace DesktopUtility.Services;

internal sealed class SettingsPersistence : IDisposable
{
    private readonly SettingsStore store;
    private readonly WallpaperSession session;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool dirty;

    public SettingsPersistence(SettingsStore store, WallpaperSession session)
    {
        this.store = store;
        this.session = session;
        timer.Tick += (_, _) => Flush();
        session.Changed += QueueSave;
    }

    private void QueueSave()
    {
        dirty = true;
        timer.Stop();
        timer.Start();
    }

    private void Flush()
    {
        timer.Stop();

        if (!dirty)
        {

            return;
        }

        try
        {
            store.Save(session.Settings);
            dirty = false;

            if (session.MessageKey == "SettingsError")
            {
                session.MessageKey = null;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            session.MessageKey = "SettingsError";
            Trace.TraceWarning(error.ToString());
        }

        session.NotifyState();
    }

    public void Dispose()
    {
        session.Changed -= QueueSave;
        Flush();
    }
}
