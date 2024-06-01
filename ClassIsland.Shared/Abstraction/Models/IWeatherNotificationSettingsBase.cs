using ClassIsland.Shared.Enums;

namespace ClassIsland.Shared.Abstraction.Models;

public interface IWeatherNotificationSettingsBase
{
    public NotificationModes AlertShowMode { get; set; }

    public NotificationModes ForecastShowMode { get; set; }
}