using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ActionSettings;

public class WindowDockingLocationActionSettings : ObservableRecipient
{
    int _value = 2;
    public int Value
    {
        get => _value;
        set
        {
            if (value == _value) return;
            _value = value;
            OnPropertyChanged();
        }
    }
}