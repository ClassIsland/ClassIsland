using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Rules;

namespace ClassIsland.Controls.RuleSettingsControls;

/// <summary>
/// CurrentWeatherRuleSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class CurrentWeatherRuleSettingsControl : RuleSettingsControlBase<CurrentWeatherRuleSettings>
{
    public IWeatherService WeatherService { get; }

    public CurrentWeatherRuleSettingsControl(IWeatherService weatherService)
    {
        WeatherService = weatherService;
        InitializeComponent();
    }
}