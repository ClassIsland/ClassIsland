using ClassIsland.Enums;

namespace ClassIsland.Interfaces;

public interface IWeatherNotificationSettingsBase
{
    public NotificationModes AlertShowMode { get; set; }

    public NotificationModes ForecastShowMode { get; set; }
}