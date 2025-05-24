using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Models.Actions;

public class SleepActionSettings : ObservableRecipient
{
    double _value = 5;
    public double Value
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