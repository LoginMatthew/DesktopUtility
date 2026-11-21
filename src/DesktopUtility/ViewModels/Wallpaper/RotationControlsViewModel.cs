namespace DesktopUtility.ViewModels;

internal sealed class RotationControlsViewModel(WallpaperSession session)
{
    public bool Enabled
    {
        get => session.Settings.RotationEnabled;
        set => session.SetRotationEnabled(value);
    }

    public int IntervalMinutes => session.Settings.IntervalMinutes;

    public event Action? Changed
    {
        add => session.ScheduleChanged += value;
        remove => session.ScheduleChanged -= value;
    }

    public bool UpdateInterval(string input)
    {

        if (!int.TryParse(input, out int minutes) || minutes is < 1 or > 1440)
        {
            session.MessageKey = "BadInterval";
            session.NotifyState();

            return false;
        }

        session.SetInterval(minutes);

        return true;
    }
}
