using ClassIsland.Interfaces;
using ClassIsland.Models.AttachedSettings;
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