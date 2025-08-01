using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ActionSettings;

public class WindowLayerActionSettings : ObservableRecipient
{
    int _value = 1;
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