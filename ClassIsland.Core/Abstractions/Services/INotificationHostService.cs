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

    /// <summary>
    /// 显示提醒。
    /// </summary>
    /// <param name="request">提醒请求</param>
    /// <remarks>注意：此方法必须由提醒主机调用。</remarks>
    [Obsolete("请使用 v2 提醒 API。")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    void ShowNotification(ClassIsland.Shared.Models.Notification.NotificationRequest request);

    /// <summary>
    /// 显示提醒，并等待提醒显示完成。
    /// </summary>
    /// <param name="request">提醒请求</param>
    /// <remarks>注意：此方法必须由提醒主机调用。</remarks>
    [Obsolete("请使用 v2 提醒 API。")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    Task ShowNotificationAsync(ClassIsland.Shared.Models.Notification.NotificationRequest request);

    internal void ShowNotification(NotificationRequest request, Guid providerGuid, Guid channelGuid);

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
}