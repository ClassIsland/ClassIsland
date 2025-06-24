using Windows.Win32.UI.Accessibility;
using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 窗口规则服务，处理窗口相关的规则。
/// </summary>
public interface IWindowRuleService
{
    /// <summary>
    /// 当焦点窗口发生变化时触发
    /// </summary>
    event EventHandler<ForegroundWindowChangedEventArgs>? ForegroundWindowChanged;

    /// <summary>
    /// 焦点窗口的 HWND。
    /// </summary>
    nint ForegroundHwnd { get; set; }

    /// <summary>
    /// 判断前台窗口是否属于 ClassIsland。
    /// </summary>
    bool IsForegroundWindowClassIsland();
}