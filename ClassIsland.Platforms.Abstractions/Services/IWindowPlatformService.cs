using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
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
    
    /// <summary>
    /// 取消注册前台窗口变化事件。
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void UnregisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler);

    /// <summary>
    /// 获取指定窗口的标题。
    /// </summary>
    /// <param name="handle">窗口句柄</param>
    /// <returns>获取到的标题</returns>
    public string GetWindowTitle(nint handle);

    /// <summary>
    /// 获取指定窗口的类名。
    /// </summary>
    /// <param name="handle">窗口句柄</param>
    /// <returns>获取到的类名</returns>
    public string GetWindowClassName(nint handle);

    /// <summary>
    /// 获取窗口是否最大化。
    /// </summary>
    /// <param name="handle">窗口句柄</param>
    /// <returns>返回指定窗口是否为最大化状态</returns>
    public bool IsWindowMaximized(nint handle);
    
    /// <summary>
    /// 获取窗口是否最小化。
    /// </summary>
    /// <param name="handle">窗口句柄</param>
    /// <returns>返回指定窗口是否为最小化状态</returns>
    public bool IsWindowMinimized(nint handle);

    /// <summary>
    /// 获取窗口是否在指定屏幕上全屏。
    /// </summary>
    /// <param name="screen">目标屏幕</param>
    /// <returns>返回前台窗口是否为全屏状态</returns>
    public bool IsForegroundWindowFullscreen(Screen screen);

    /// <summary>
    /// 获取前台窗口是否在指定屏幕上最大化。
    /// </summary>
    /// <param name="screen">目标屏幕</param>
    /// <returns>返回前台窗口是否为最大化状态</returns>
    public bool IsForegroundWindowMaximized(Screen screen);

    /// <summary>
    /// 获取当前指针位置
    /// </summary>
    /// <returns>指针位置</returns>
    public Point GetMousePos();
    
    /// <summary>
    /// 获取当前前台窗口句柄
    /// </summary>
    public nint ForegroundWindowHandle { get; }

    /// <summary>
    /// 获取窗口对应的进程 ID
    /// </summary>
    /// <param name="handle">窗口句柄</param>
    /// <returns>目标 PID</returns>
    public int GetWindowPid(nint handle);
}