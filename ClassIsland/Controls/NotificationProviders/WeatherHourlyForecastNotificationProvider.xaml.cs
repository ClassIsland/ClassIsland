using System;
using System.Collections.Generic;
using System.Windows.Controls;
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