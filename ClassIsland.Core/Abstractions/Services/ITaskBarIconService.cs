using H.NotifyIcon;

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
}