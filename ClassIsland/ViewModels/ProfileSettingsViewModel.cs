using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class ProfileSettingsViewModel : ObservableRecipient
{
    private object _drawerContent = new();

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
}