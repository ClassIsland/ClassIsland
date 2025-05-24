using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Actions;

public class WindowLayerActionSettings : ObservableRecipient
{
    private int _value = 1;

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