using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Actions;

public class CurrentComponentConfigActionSettings : ObservableRecipient
{
    private string _value = "Default";

    public string Value
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