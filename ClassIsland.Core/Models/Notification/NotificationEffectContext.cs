using Avalonia;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 表示用于生成提醒特效的上下文。
/// </summary>
public class NotificationEffectContext
{
    /// <summary>
    /// 提醒效果的屏幕中心点
    /// </summary>
    public required PixelPoint Center { get; init; }
    /// <summary>
    /// 发起此提醒效果的提醒内容
    /// </summary>
    public required NotificationContent Content { get; init; }
    /// <summary>
    /// 发起此提醒效果的提醒请求
    /// </summary>
    public required NotificationRequest Request { get; init; }
}