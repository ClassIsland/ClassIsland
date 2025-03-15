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
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// WeatherHourlyForecastNotificationProvider.xaml 的交互逻辑
/// </summary>
public partial class WeatherHourlyForecastNotificationProvider : UserControl
{
    public bool IsOverlay { get; }
    public WeatherInfo Info { get; }
    public DateTime BaseTime { get; }

    public List<DateTime> DateTimes { get; } = [];

    public WeatherHourlyForecastNotificationProvider(bool isOverlay, WeatherInfo info, DateTime baseTime)
    {
        IsOverlay = isOverlay;
        Info = info;
        BaseTime = baseTime;
        for (var i = 0; i < 6; i++)
        {
            DateTimes.Add(baseTime.AddHours(i));
        }
        InitializeComponent();
    }
}