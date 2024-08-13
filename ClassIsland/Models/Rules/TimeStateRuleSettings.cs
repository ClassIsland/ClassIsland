using ClassIsland.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Rules;

public class TimeStateRuleSettings : ObservableRecipient
{
    private TimeState _state = TimeState.OnClass;

    public TimeState State
    {
        get => _state;
        set
        {
            if (value == _state) return;
            _state = value;
            OnPropertyChanged();
        }
    }
}