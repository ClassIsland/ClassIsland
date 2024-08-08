using System;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// WeatherNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.TimePoint)]
[AttachedSettingsControlInfo("7625DE96-38AA-4B71-B478-3F156DD9458D", "天气提醒设置", PackIconKind.WeatherCloudy, false)]
public partial class WeatherNotificationAttachedSettingsControl
{
    public WeatherNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }
}