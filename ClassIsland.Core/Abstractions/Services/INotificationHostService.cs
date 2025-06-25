using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Hosting;
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 提醒主机服务，用于管理提醒提供方和发布提醒。
/// </summary>
public interface INotificationHostService : IHostedService, INotifyPropertyChanged
{
    internal PriorityQueue<NotificationRequest, NotificationPriority> RequestQueue { get; }
    internal ObservableCollection<NotificationProviderRegisterInfo> NotificationProviders { get; }
    
    internal NotificationRequest? CurrentRequest { get; set; }
    
    internal NotificationRequest GetRequest();

    /// <summary>
    /// 注册提醒服务。
    /// </summary>
    /// <param name="provider">要注册的服务实例。</param>
    /// <example>
    /// <code>
    /// NotificationHostService.RegisterNotificationProvider(this);
    /// </code>
    /// </example>
    void RegisterNotificationProvider(INotificationProvider provider);

    internal void ShowNotification(NotificationRequest request, Guid providerGuid, Guid channelGuid, bool pushNotifications);

    internal Task ShowNotificationAsync(NotificationRequest request, Guid providerGuid, Guid channelGuid);

    internal void ShowChainedNotifications(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid);

    internal Task ShowChainedNotificationsAsync(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid);

    internal void RegisterNotificationChannel(NotificationChannel channel);

    /// <summary>
    /// 获取提醒服务设置实例。如果此提醒提供方设置不存在，则会新建并保存一个实例指定的设置实例。
    /// </summary>
    /// <typeparam name="T">提醒提供方设置类型</typeparam>
    /// <param name="id">提醒服务id</param>
    /// <returns>对应提醒服务设置实例。若不存在，则返回默认值。</returns>
    T GetNotificationProviderSettings<T>(Guid id) where T : class;

    /// <summary>
    /// 保存提醒提供方设置。
    /// </summary>
    /// <typeparam name="T">提醒提供方设置类型</typeparam>
    /// <param name="id">提醒提供方 ID</param>
    /// <param name="settings">要保存的设置</param>
    void WriteNotificationProviderSettings<T>(Guid id, T settings);

    internal void CancelAllNotifications();

    /// <summary>
    /// 注册提醒消费者。
    /// </summary>
    /// <param name="consumer">要注册的提醒消费者</param>
    /// <param name="priority">提醒消费者优先级</param>
    public void RegisterNotificationConsumer(INotificationConsumer consumer, int priority);

    /// <summary>
    /// 取消注册提醒消费者。
    /// </summary>
    /// <param name="consumer">要注销的提醒消费者</param>
    public void UnregisterNotificationConsumer(INotificationConsumer consumer);

    /// <summary>
    /// 拉取要播放的提醒。
    /// </summary>
    /// <remarks>
    /// 此方法一般情况下只会返回一个要显示的提醒。如果有成链的提醒，会将这些提醒一并返回。请在显示完上次显示的提醒后再调用此方法。
    /// </remarks>
    /// <returns>获得的提醒</returns>
    public IList<NotificationRequest> PullNotificationRequests();
    
    /// <summary>
    /// 当前是否正在播放提醒
    /// </summary>
    public bool IsNotificationsPlaying { get; }
}