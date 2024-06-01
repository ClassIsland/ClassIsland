using ClassIsland.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Notification;

/// <summary>
/// 提醒提供方注册信息
/// </summary>
/// <param name="providerInstance">提醒提供方实例</param>
public class NotificationProviderRegisterInfo(INotificationProvider providerInstance) : ObservableRecipient
{
    /// <summary>
    /// 提醒提供方名称
    /// </summary>
    public string Name { get; set; } = providerInstance.Name;
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = providerInstance.Description;
    /// <summary>
    /// 提供方GUID
    /// </summary>
    public Guid ProviderGuid { get; set; } = providerInstance.ProviderGuid;

    /// <summary>
    /// 提供方实例
    /// </summary>
    public INotificationProvider ProviderInstance { get; } = providerInstance;

    /// <summary>
    /// 提供方设置
    /// </summary>
    public NotificationSettings ProviderSettings { get; set; } = new();
}