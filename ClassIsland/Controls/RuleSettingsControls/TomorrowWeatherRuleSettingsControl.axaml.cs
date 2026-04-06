using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.Rules;

namespace ClassIsland.Controls.RuleSettingsControls;

/// <summary>
/// TomorrowWeatherRuleSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class TomorrowWeatherRuleSettingsControl : RuleSettingsControlBase<TomorrowWeatherRuleSettings>
{
    public IWeatherService WeatherService { get; }

    public TomorrowWeatherRuleSettingsControl(IWeatherService weatherService)
    {
        WeatherService = weatherService;
        InitializeComponent();
    }
}