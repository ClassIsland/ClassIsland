#if !NETFRAMEWORK

namespace ClassIsland.Shared.Interfaces;

/// <summary>
/// 提醒提供方接口。
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// 提醒提供方名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 提醒提供方描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 提醒提供方 GUID
    /// </summary>
    public Guid ProviderGuid { get; set; }
    /// <summary>
    /// 提醒提供方设置界面控件，将显示在【应用设置】->【提醒】中。留空则代表此提醒提供方没有设置界面。
    /// </summary>
    public object? SettingsElement { get; set; }

    /// <summary>
    /// 提醒提供方图标元素，留空则代表使用默认图标。
    /// </summary>
    public object? IconElement { get; set; }

    /// <summary>
    /// 提醒提供方默认提醒音效 Uri
    /// </summary>
    public static readonly Uri DefaultNotificationSoundUri = new Uri("pack://application:,,,/ClassIsland;component/Assets/Media/Notification/1.wav");
}
#endif