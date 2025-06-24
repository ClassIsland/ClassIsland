using Avalonia.Controls;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 窗口平台服务接口。
/// </summary>
public interface IWindowPlatformService
{
    /// <summary>
    /// 设置窗口特性。
    /// </summary>
    /// <param name="toplevel">要设置的窗口</param>
    /// <param name="features">要修改的特性</param>
    /// <param name="state">修改的特性状态</param>
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state);

    /// <summary>
    /// 获取一个窗口的窗口特性。
    /// </summary>
    /// <param name="topLevel">要获取窗口特性的窗口</param>
    /// <returns>窗口当前拥有的特性</returns>
    public WindowFeatures GetWindowFeatures(TopLevel topLevel);

    /// <summary>
    /// 注册前台窗口变化事件。
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void RegisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler);

}