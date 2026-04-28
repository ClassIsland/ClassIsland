using System.Threading.Tasks;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Abstraction.Models;

namespace ClassIsland.Core.Abstractions;

/// <summary>
/// 提醒播放处理器.
/// </summary>
public interface INotificationPlaybackHandler
{
    /// <summary>
    /// 开始播放遮罩。
    /// </summary>
    Task OnPlayMaskAsync(NotificationRequest request, INotificationSettings settings);

    /// <summary>
    /// 开始播放正文。
    /// </summary>
    Task OnPlayOverlayAsync(NotificationRequest request, INotificationSettings settings);

    /// <summary>
    /// 提醒播放完成(无票据)。
    /// </summary>
    void OnPlaybackCompleted();
}
