using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 桌面通知服务
/// </summary>
public interface IDesktopToastService
{
    /// <summary>
    /// 显示一条桌面通知。
    /// </summary>
    /// <param name="content">通知内容</param>
    Task ShowToastAsync(DesktopToastContent content);
    
    
    /// <summary>
    /// 显示一条桌面通知。
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="body">通知正文</param>
    /// <param name="activated">通知被点击时的回调方法</param>
    Task ShowToastAsync(string title, string body, Action? activated = null);

    /// <summary>
    /// 处理通知激活
    /// </summary>
    /// <param name="id">激活 id</param>
    void ActivateNotificationAction(Guid id);
}