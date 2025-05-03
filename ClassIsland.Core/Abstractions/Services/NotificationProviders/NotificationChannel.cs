using System.Windows.Controls;
using System.Windows.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using MaterialDesignThemes.Wpf;
using System.Windows.Media.Imaging;
using ClassIsland.Core.Abstractions.Controls;

namespace ClassIsland.Core.Abstractions.Services.NotificationProviders;

/// <summary>
/// 代表一个提醒发送渠道
/// </summary>
public class NotificationChannel : INotificationSender, INotificationProvider
{

    /// <summary>
    /// 当前提醒渠道所属的提醒提供方信息
    /// </summary>
    public NotificationProviderInfo ProviderInfo { get; }

    /// <summary>
    /// 当前提醒渠道信息
    /// </summary>
    public NotificationChannelInfo ChannelInfo { get; }

    /// <inheritdoc/>
    public string Name { get; set; }

    /// <inheritdoc/>
    public string Description { get; set; }

    /// <inheritdoc/>
    Guid INotificationProvider.ProviderGuid
    {
        get => ChannelGuid;
        set
        {
            // ignored
        }
    }

    /// <inheritdoc/>
    public object? SettingsElement { get; set; }

    /// <inheritdoc/>
    public object? IconElement { get; set; }
    private Guid ProviderGuid => ProviderInfo.Guid;

    private Guid ChannelGuid => ChannelInfo.Guid;

    // ReSharper disable once InconsistentNaming
    internal INotificationHostService __NotificationHostService { get; } = IAppHost.GetService<INotificationHostService>();

    internal NotificationChannel(NotificationProviderBase provider, NotificationProviderInfo providerInfo, NotificationChannelInfo channelInfo)
    {
        ProviderInfo = providerInfo;
        ChannelInfo = channelInfo;


        var info = ChannelInfo;
        Name = info.Name;
        Description = info.Description;
        IconElement = new PackIcon()
        {
            Kind = info.PackIcon,
            Width = 24,
            Height = 24
        };

        __NotificationHostService.RegisterNotificationChannel(this);

        if (info.SettingsControlType == null) 
            return;
        var settings = provider.SettingsInternal;
        SettingsElement = NotificationProviderControlBase.GetInstance(info, ref settings);
        provider.SettingsInternal = settings!;
    }

    /// <inheritdoc />
    public void ShowNotification(NotificationRequest request)
    {
        __NotificationHostService.ShowNotification(request, ProviderGuid, ChannelGuid);
    }

    /// <inheritdoc />
    public async Task ShowNotificationAsync(NotificationRequest request)
    {
        await __NotificationHostService.ShowNotificationAsync(request, ProviderGuid, ChannelGuid);
    }

    /// <inheritdoc />
    public void ShowChainedNotifications(params NotificationRequest[] requests)
    {
        __NotificationHostService.ShowChainedNotifications(requests, ProviderGuid, ChannelGuid);
    }

    /// <inheritdoc />
    public async Task ShowChainedNotificationsAsync(NotificationRequest[] requests)
    {
        await __NotificationHostService.ShowChainedNotificationsAsync(requests, ProviderGuid, ChannelGuid);
    }
}