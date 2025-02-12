using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Rules;

public partial class RainTimeRuleSettings : ObservableObject
{
    [ObservableProperty] private double _rainTimeMinutes = 60;

    [ObservableProperty] private bool _isRemainingTime = false;
}