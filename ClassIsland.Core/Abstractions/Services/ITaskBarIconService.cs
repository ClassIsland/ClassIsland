using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 任务栏图标服务
/// </summary>
public interface ITaskBarIconService
{
    /// <summary>
    /// 任务栏图标实例
    /// </summary>
    TaskbarIcon MainTaskBarIcon { get; }

    /// <summary>
    /// 显示一条气泡通知。
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="content">通知正文</param>
    /// <param name="icon">气泡图标</param>
    /// <param name="clickedCallback">点击通知后执行的操作</param>
    public void ShowNotification(string title, string content, NotificationIcon icon=NotificationIcon.None, Action? clickedCallback=null);
}