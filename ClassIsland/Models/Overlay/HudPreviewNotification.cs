using System;
using ClassIsland.Models.Actions;

namespace ClassIsland.Models.Overlay;

/// <summary>
/// 主 App 转发给悬浮主界面宿主进程的提醒预览请求。
/// </summary>
/// <remarks>
/// 仅在手动预览（自动化编辑页点击"触发"）时产生。宿主进程读取后使用与
/// <see cref="NotificationActionSettings"/> 相同的内容在悬浮岛上显示提醒。
/// </remarks>
public class HudPreviewNotification
{
    /// <summary>
    /// 预览类型。目前仅支持 <c>showNotification</c>（显示提醒）。
    /// </summary>
    public string Kind { get; set; } = "showNotification";

    /// <summary>
    /// 请求创建时间。宿主进程会丢弃过期的预览请求。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 用于显示提醒的行动设置。
    /// </summary>
    public NotificationActionSettings Settings { get; set; } = new();
}
