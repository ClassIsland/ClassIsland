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

    private bool _showRealTime = false;

    public bool ShowRealTime
    {
        get => _showRealTime;
        set
        {
            if (value == _showRealTime) return;
            _showRealTime = value;
            OnPropertyChanged();
        }
    }

    private bool _flashTimeSeparator = true;

    public bool FlashTimeSeparator
    {
        get => _flashTimeSeparator;
        set
        {
            if (value == _flashTimeSeparator) return;
            _flashTimeSeparator = value;
            OnPropertyChanged();
        }
    }
}