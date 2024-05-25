using ClassIsland.Core.Interfaces;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

public class NotificationProviderRegisterInfo(INotificationProvider providerInstance) : ObservableRecipient
{
    public string Name { get; set; } = providerInstance.Name;
    public string Description { get; set; } = providerInstance.Description;

    public Guid ProviderGuid { get; set; } = providerInstance.ProviderGuid;


    public INotificationProvider ProviderInstance { get; } = providerInstance;

    public NotificationSettings ProviderSettings { get; set; } = new();
}