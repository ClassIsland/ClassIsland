using System;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class TimeAdjustmentViewModel : ObservableObject
{
    [ObservableProperty] private DateTime _currentTime = DateTime.Now;

    [ObservableProperty] private bool _isTimeAdjusted = false;

    [ObservableProperty] private bool _isClockOpen = false;
    private DateTime _targetTime = DateTime.Now;

    public DateTime TargetTime
    {
        get => _targetTime;
        set
        {
            if (value.Equals(_targetTime)) return;
            _targetTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TargetTimeSpan));
        }
    }

    public TimeSpan TargetTimeSpan
    {
        get => TargetTime.TimeOfDay;
        set => TargetTime = new DateTime(DateOnly.FromDateTime(TargetTime.Date), TimeOnly.FromTimeSpan(value));
    }
}