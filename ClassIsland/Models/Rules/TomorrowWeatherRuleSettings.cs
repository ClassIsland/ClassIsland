using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Rules;

public partial class TomorrowWeatherRuleSettings : ObservableObject
{
    [ObservableProperty] private int _weatherId = 0;
}