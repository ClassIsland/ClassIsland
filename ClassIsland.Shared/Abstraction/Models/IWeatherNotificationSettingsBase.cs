using ClassIsland.Shared.Enums;

namespace ClassIsland.Shared.Abstraction.Models;

/// <summary>
/// 天气提醒提供方设置接口
/// </summary>
public interface IWeatherNotificationSettingsBase
{
    /// <summary>
    /// 气象预警显示模式
    /// </summary>
    public NotificationModes AlertShowMode { get; set; }

    /// <summary>
    /// 天气预报显示模式
    /// </summary>
    public NotificationModes ForecastShowMode { get; set; }
}