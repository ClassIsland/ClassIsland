using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Models.Actions;

public class RunActionSettings : ObservableRecipient
{
    string _value = "";
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

    string _args = "";
    public string Args
    {
        get => _args;
        set
        {
            if (value == _args) return;
            _args = value;
            OnPropertyChanged();
        }
    }
}