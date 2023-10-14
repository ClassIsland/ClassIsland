using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class WeatherNotificationProviderSettings : ObservableRecipient
{
    public bool IsWeatherAlertsNotifyEnabled { get; set; } = true;

    public bool IsWeatherForecastEnabled { get; set; } = true;
}