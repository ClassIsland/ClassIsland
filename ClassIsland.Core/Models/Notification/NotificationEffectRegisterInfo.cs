using ClassIsland.Shared.Interfaces.Controls;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 表示提醒效果的注册信息。
/// </summary>
public class NotificationEffectRegisterInfo
{
    internal NotificationEffectRegisterInfo() { }
    
    /// <summary>
    /// 提醒效果的标识符。
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// 提醒效果的显示名称。
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 根据提醒效果上下文创建效果控件的工厂委托。
    /// </summary>
    /// <remarks>
    /// 接收 <see cref="NotificationEffectContext"/>，返回实现 <see cref="INotificationEffectControl"/> 的效果控件；
    /// 返回 <see langword="null"/> 表示不创建效果控件。
    /// </remarks>
    public required Func<NotificationEffectContext, INotificationEffectControl?> Factory { get; init; }
}
