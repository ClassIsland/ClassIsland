using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Rules;

public partial class CurrentWeatherRuleSettings : ObservableObject
{
    [ObservableProperty] private int _weatherId = 0;
}