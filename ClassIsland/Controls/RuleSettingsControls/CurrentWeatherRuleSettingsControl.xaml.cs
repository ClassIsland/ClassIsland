using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Controls.RuleSettingsControls;

/// <summary>
/// CurrentWeatherRuleSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class CurrentWeatherRuleSettingsControl
{
    public IWeatherService WeatherService { get; }

    public CurrentWeatherRuleSettingsControl(IWeatherService weatherService)
    {
        WeatherService = weatherService;
        InitializeComponent();
    }
}