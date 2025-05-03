#if !NETFRAMEWORK
using ClassIsland.Shared.Interfaces;
using System.ComponentModel;
using ClassIsland.Shared.Models.Notification;

namespace ClassIsland.Shared.Abstraction.Models.Notification;

/// <summary>
/// 代表可发送提醒的提供方注册信息。
/// </summary>
public interface INotificationSenderRegisterInfo : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// 提醒提供方名称
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// 提供方GUID
    /// </summary>
    Guid ProviderGuid { get; set; }

    /// <summary>
    /// 提供方实例
    /// </summary>
    INotificationProvider ProviderInstance { get; }

    /// <summary>
    /// 提供方设置
    /// </summary>
    NotificationSettings ProviderSettings { get; set; }
}
#endif