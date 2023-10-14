using System.Collections.Generic;
using ClassIsland.Enums;
using ClassIsland.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public class WeatherNotificationProviderSettings : ObservableRecipient, IWeatherNotificationSettingsBase
{
    public NotificationModes AlertShowMode { get; set; }
    public NotificationModes ForecastShowMode { get; set; }
}