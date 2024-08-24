using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ComponentSettings;

public class ClockComponentSettings: ObservableRecipient
{
    private bool _showSeconds = false;

    public bool ShowSeconds
    {
        get => _showSeconds;
        set
        {
            if (value == _showSeconds) return;
            _showSeconds = value;
            OnPropertyChanged();
        }
    }
}