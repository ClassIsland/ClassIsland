using Windows.Win32.UI.Accessibility;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 窗口规则服务，处理窗口相关的规则。
/// </summary>
public interface IWindowRuleService
{
    /// <summary>
    /// 当焦点窗口发生变化时触发
    /// </summary>
    event WINEVENTPROC? ForegroundWindowChanged;

    /// <summary>
    /// 焦点窗口的 HWND。
    /// </summary>
    HWND ForegroundHwnd { get; set; }
}