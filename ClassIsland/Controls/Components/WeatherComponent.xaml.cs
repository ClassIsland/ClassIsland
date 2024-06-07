using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// WeatherComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("CA495086-E297-4BEB-9603-C5C1C1A8551E", "天气简报", PackIconKind.WeatherSunny, "显示当前的天气概况和气象预警。")]
public partial class WeatherComponent : ComponentBase<WeatherComponentSettings>
{
    public IWeatherService WeatherService { get; }

    public SettingsService SettingsService { get; }

    public WeatherComponent(IWeatherService weatherService, SettingsService settingsService)
    {
        WeatherService = weatherService;
        SettingsService = settingsService;
        InitializeComponent();
    }
}