using System.ComponentModel;

namespace DesktopUtility.ViewModels;

internal sealed class RotationStatusViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly WallpaperSession session;
    private readonly Language words;

    public RotationStatusViewModel(WallpaperSession session, Language words)
    {
        this.session = session;
        this.words = words;
        session.StateUpdated += Refresh;
    }

    public string Status
    {
        get
        {

            if (!session.Settings.RotationEnabled)
            {

                return words["Paused"];
            }

            return session.Remaining is { } remaining
                ? $"{words["Next"]} · {TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(remaining.TotalSeconds))):g}"
                : words["NoCandidates"];
        }
    }

    public string Notice => session.MessageKey is { } message ? words[message] : "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

    public void Dispose() => session.StateUpdated -= Refresh;
}
