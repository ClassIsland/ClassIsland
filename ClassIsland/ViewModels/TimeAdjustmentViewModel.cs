using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class TimeAdjustmentViewModel : ObservableObject
{
    [ObservableProperty] private DateTime _targetTime = DateTime.Now;

    [ObservableProperty] private DateTime _currentTime = DateTime.Now;

    [ObservableProperty] private bool _isTimeAdjusted = false;

    [ObservableProperty] private bool _isClockOpen = false;
}