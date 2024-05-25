using System.Windows.Controls;

using ClassIsland.Models.NotificationProviderSettings;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// WeatherNotificationProviderSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherNotificationProviderSettingsControl : UserControl
{
    public WeatherNotificationProviderSettings Settings { get; }

    public WeatherNotificationProviderSettingsControl(WeatherNotificationProviderSettings settings)
    {
        Settings = settings;
        InitializeComponent();
    }
}