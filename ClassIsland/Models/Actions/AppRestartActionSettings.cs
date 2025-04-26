using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Models.Actions;

public class AppRestartActionSettings : ObservableRecipient
{
    bool _silent = false;
    public bool Silent
    {
        get => _silent;
        set
        {
            if (value == _silent) return;
            _silent = value;
            OnPropertyChanged();
        }
    }
}