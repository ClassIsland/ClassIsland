using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 平台窗口服务桩
/// </summary>
public class WindowPlatformServiceStub : IWindowPlatformService
{
    internal WindowPlatformServiceStub()
    {
        
    }
    
    /// <inheritdoc />
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
    }

    /// <inheritdoc />
    public WindowFeatures GetWindowFeatures(TopLevel topLevel)
    {
        return (WindowFeatures)0;
    }

    /// <inheritdoc />
    public void RegisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {
    }

    /// <inheritdoc />
    public void UnregisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {
    }

    /// <inheritdoc />
    public string GetWindowTitle(IntPtr handle)
    {
        return "";
    }

    /// <inheritdoc />
    public string GetWindowClassName(IntPtr handle)
    {
        return "";
    }

    /// <inheritdoc />
    public bool IsWindowMaximized(IntPtr handle)
    {
        return false;
    }

    /// <inheritdoc />
    public bool IsWindowMinimized(IntPtr handle)
    {
        return false;
    }

    /// <inheritdoc />
    public bool IsForegroundWindowFullscreen(Screen screen)
    {
        return false;
    }

    /// <inheritdoc />
    public bool IsForegroundWindowMaximized(Screen screen)
    {
        return false;
    }

    /// <inheritdoc />
    public Point GetMousePos()
    {
        return new Point();
    }

    /// <inheritdoc />
    public IntPtr ForegroundWindowHandle { get; } = nint.Zero;

    /// <inheritdoc />
    public int GetWindowPid(IntPtr handle)
    {
        return 0;
    }
}