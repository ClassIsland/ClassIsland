using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 提醒主机服务
/// </summary>
public interface INotificationHostService
{
    PriorityQueue<NotificationRequest, int> RequestQueue { get; }
    ObservableCollection<NotificationProviderRegisterInfo> NotificationProviders { get; }
    NotificationRequest? CurrentRequest { get; set; }
    NotificationRequest GetRequest();

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

    void ShowNotification(NotificationRequest request);
    Task ShowNotificationAsync(NotificationRequest request);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取提醒服务实例。
    /// </summary>
    /// <typeparam name="T">提醒服务类型</typeparam>
    /// <param name="id">提醒服务id</param>
    /// <returns>对应提醒服务实例。若不存在，则返回null。</returns>
    T? GetNotificationProviderSettings<T>(Guid id);

    void WriteNotificationProviderSettings<T>(Guid id, T settings);
    event PropertyChangedEventHandler? PropertyChanged;
}