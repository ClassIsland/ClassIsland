using System.Collections.ObjectModel;

using ClassIsland.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class WindowsPickerViewModel : ObservableRecipient
{
    private ObservableCollection<DesktopWindow> _desktopWindows = new();
    private string _selectedClassName = "";
    private bool _isWorking = false;
    private bool _isFilteredFullscreen = true;

    public ObservableCollection<DesktopWindow> DesktopWindows
    {
        get => _desktopWindows;
        set
        {
            if (Equals(value, _desktopWindows)) return;
            _desktopWindows = value;
            OnPropertyChanged();
        }
    }

    public string SelectedClassName
    {
        get => _selectedClassName;
        set
        {
            if (value == _selectedClassName) return;
            _selectedClassName = value;
            OnPropertyChanged();
        }
    }

    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            if (value == _isWorking) return;
            _isWorking = value;
            OnPropertyChanged();
        }
    }

    public bool IsFilteredFullscreen
    {
        get => _isFilteredFullscreen;
        set
        {
            if (value == _isFilteredFullscreen) return;
            _isFilteredFullscreen = value;
            OnPropertyChanged();
        }
    }
}