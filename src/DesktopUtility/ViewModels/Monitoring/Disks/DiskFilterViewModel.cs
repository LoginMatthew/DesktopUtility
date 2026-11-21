using System.ComponentModel;
using DesktopUtility.Core.Monitoring;

namespace DesktopUtility.ViewModels;

internal sealed class DiskFilterViewModel(Settings settings, Language language, Action save) : INotifyPropertyChanged
{
    public Language Words => language;
    public IReadOnlyList<DriveOptionViewModel> Drives { get; private set; } = [];

    public bool AllDrives
    {
        get => settings.MonitorAllDisks;
        set
        {

            if (settings.MonitorAllDisks == value)
            {

                return;
            }

            if (!value)
            {
                SelectConnectedDrives();
            }

            settings.MonitorAllDisks = value;
            CommitSelection();
        }
    }

    public event EventHandler? SelectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Includes(string root) => settings.MonitorAllDisks || settings.MonitoredDriveRoots.Contains(root);

    public void UpdateDrives(IReadOnlyList<DiskVolumeReading> volumes)
    {
        var connected = volumes.OrderBy(volume => volume.Name, StringComparer.OrdinalIgnoreCase)
            .Select(volume => (Root: volume.Name, Name: volume.DisplayName)).ToArray();

        if (!Drives.Select(drive => (drive.Root, drive.Name)).SequenceEqual(connected))
        {
            Drives = connected.Select(drive => new DriveOptionViewModel(this, drive.Root, drive.Name)).ToArray();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Drives)));
        }
    }

    public void SetDrive(string root, bool selected)
    {

        if (Includes(root) == selected)
        {

            return;
        }

        if (AllDrives)
        {
            SelectConnectedDrives();
            settings.MonitorAllDisks = false;
        }

        if (selected)
        {
            settings.MonitoredDriveRoots.Add(root);
        }
        else
        {
            settings.MonitoredDriveRoots.Remove(root);
        }

        CommitSelection();
    }

    public void RefreshLanguage() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Words)));

    private void SelectConnectedDrives()
    {
        settings.MonitoredDriveRoots.Clear();
        settings.MonitoredDriveRoots.UnionWith(Drives.Select(drive => drive.Root));
    }

    private void CommitSelection()
    {
        save();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllDrives)));

        foreach (var drive in Drives)
        {
            drive.RefreshSelection();
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
