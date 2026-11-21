using System.ComponentModel;

namespace DesktopUtility.ViewModels;

internal sealed class DriveOptionViewModel(DiskFilterViewModel filter, string root, string name) : INotifyPropertyChanged
{
    public string Root => root;
    public string Name => name;

    public bool Selected
    {
        get => filter.Includes(Root);
        set => filter.SetDrive(Root, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshSelection() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
}
