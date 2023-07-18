using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class ProfileSettingsViewModel : ObservableRecipient
{
    private object _drawerContent = new();
    private bool _isClassPlansEditing = false;

    public object DrawerContent
    {
        get => _drawerContent;
        set
        {
            if (Equals(value, _drawerContent)) return;
            _drawerContent = value;
            OnPropertyChanged();
        }
    }

    public bool IsClassPlansEditing
    {
        get => _isClassPlansEditing;
        set
        {
            if (value == _isClassPlansEditing) return;
            _isClassPlansEditing = value;
            OnPropertyChanged();
        }
    }
}