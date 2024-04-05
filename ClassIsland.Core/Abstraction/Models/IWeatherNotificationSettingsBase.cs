using ClassIsland.Core.Enums;

namespace ClassIsland.Core.Abstraction.Models;

public interface IWeatherNotificationSettingsBase
{
    public NotificationModes AlertShowMode { get; set; }

    public NotificationModes ForecastShowMode { get; set; }
}