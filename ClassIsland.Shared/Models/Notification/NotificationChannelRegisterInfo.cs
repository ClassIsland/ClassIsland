#if !NETFRAMEWORK
using ClassIsland.Shared.Abstraction.Models.Notification;
using ClassIsland.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ClassIsland.Shared.Models.Notification;

/// <summary>
/// 提醒提供方注册信息
/// </summary>
/// <param name="providerInstance">提醒提供方实例</param>
public class NotificationChannelRegisterInfo(INotificationProvider providerInstance) : ObservableRecipient, INotificationSenderRegisterInfo
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

    /// <inheritdoc/>
    public NotificationSettings ProviderSettings
    {
        get => _providerSettings;
        set => SetProperty(ref _providerSettings, value);
    }
}
#endif