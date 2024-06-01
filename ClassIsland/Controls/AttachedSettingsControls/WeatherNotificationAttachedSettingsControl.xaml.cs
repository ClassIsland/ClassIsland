using System;
using System.Windows.Controls;

using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// WeatherNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class WeatherNotificationAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public WeatherNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } =
        new AttachedSettingsControlHelper<WeatherNotificationAttachedSettings>(
            new Guid("7625DE96-38AA-4B71-B478-3F156DD9458D"), new WeatherNotificationAttachedSettings());

    public WeatherNotificationAttachedSettings Settings =>
        ((AttachedSettingsControlHelper<WeatherNotificationAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings ?? new WeatherNotificationAttachedSettings();
}