namespace ClassIsland.Shared.Abstraction.Models;

/// <summary>
/// 放学提醒提供方设置接口
/// </summary>
public interface IAfterSchoolNotificationProviderSettingsBase
{
    /// <summary>
    /// 是否启用放学提醒
    /// </summary>
    public bool IsEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// 放学提醒文本
    /// </summary>
    public string NotificationMsg
    {
        get;
        set;
    }
}