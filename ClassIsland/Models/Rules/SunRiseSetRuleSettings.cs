using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Rules;

public partial class SunRiseSetRuleSettings : ObservableObject
{
    [ObservableProperty] private double _timeMinutes = 60;

    [ObservableProperty] private bool _isSunset = false;
}
