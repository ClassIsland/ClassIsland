using ClassIsland.Core.Models.Notification;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 提醒工作服务
/// </summary>
public interface INotificationWorkerService
{
    internal NotificationPlayingTicket CreateTicket(NotificationRequest request);

    /// <summary>
    /// 取消所有正在播放的音效和语音。
    /// </summary>
    void CancelAllAudio();
}