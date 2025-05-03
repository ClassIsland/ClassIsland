#if !NETFRAMEWORK
using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Shared.Abstraction.Models.Notification;
using ClassIsland.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Notification;

/// <summary>
/// 提醒提供方注册信息
/// </summary>
/// <param name="providerInstance">提醒提供方实例</param>
public class NotificationProviderRegisterInfo(INotificationProvider providerInstance) : ObservableRecipient, INotificationSenderRegisterInfo
{
    private string _name = providerInstance.Name;
    private string _description = providerInstance.Description;
    private Guid _providerGuid = providerInstance.ProviderGuid;
    private NotificationSettings _providerSettings = new();

    /// <inheritdoc/>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <inheritdoc/>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <inheritdoc/>
    public Guid ProviderGuid
    {
        get => _providerGuid;
        set => SetProperty(ref _providerGuid, value);
    }

    /// <inheritdoc/>
    public INotificationProvider ProviderInstance { get; } = providerInstance;

    /// <summary>
    /// 包含的提醒渠道实例
    /// </summary>
    public ObservableCollection<NotificationChannelRegisterInfo> NotificationChannels { get; } = [];

    /// <inheritdoc/>
    public NotificationSettings ProviderSettings
    {
        get => _providerSettings;
        set => SetProperty(ref _providerSettings, value);
    }
}
#endif