using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class DebugPageViewModel : ObservableRecipient
{
    private bool _isTargetDateTimeLoaded = false;
    private DateTime _targetDate = new();
    private DateTime _targetTime = new();

    public bool IsTargetDateTimeLoaded
    {
        get => _isTargetDateTimeLoaded;
        set
        {
            if (value == _isTargetDateTimeLoaded) return;
            _isTargetDateTimeLoaded = value;
            OnPropertyChanged();
        }
    }

    public DateTime TargetDate
    {
        get => _targetDate;
        set
        {
            if (value == _targetDate) return;
            _targetDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime TargetTime
    {
        get => _targetTime;
        set
        {
            if (value == _targetTime) return;
            _targetTime = value;
            OnPropertyChanged();
        }
    }
}