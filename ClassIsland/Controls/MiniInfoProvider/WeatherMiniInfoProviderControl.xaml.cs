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
using ClassIsland.Models;
using ClassIsland.Services;

namespace ClassIsland.Controls.MiniInfoProvider;

/// <summary>
/// WeatherMiniInfoProviderControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherMiniInfoProviderControl : UserControl
{
    private SettingsService SettingsService { get; }

    public Settings AppSettings => SettingsService.Settings;

    public WeatherMiniInfoProviderSettings Settings { get; }

    public WeatherMiniInfoProviderControl(SettingsService settingsService, WeatherMiniInfoProviderSettings weatherMiniInfoProviderSettings)
    {
        Settings = weatherMiniInfoProviderSettings;
        SettingsService = settingsService;
        InitializeComponent();
    }
}