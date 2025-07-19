using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.MacOs.Services;

public class WindowPlatformServices : IWindowPlatformService
{
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
        var win = NSApplication.SharedApplication.WindowWithWindowNumber(toplevel.TryGetPlatformHandle()!.Handle);
        
        
        if ((features & WindowFeatures.Transparent) > 0)
        {
            win.IgnoresMouseEvents = state;
        }
        if ((features & WindowFeatures.Bottommost) > 0)
        {
            
        }
        if ((features & WindowFeatures.Topmost) > 0)
        {
            
        }
        if ((features & WindowFeatures.Private) > 0)
        {
            
        }
        if ((features & WindowFeatures.ToolWindow) > 0 && toplevel is Window window)
        {
            
        }
        if ((features & WindowFeatures.SkipManagement) > 0)
        {
            
        }
    }

    public WindowFeatures GetWindowFeatures(TopLevel topLevel)
    {
        return WindowFeatures.None;
    }

    public void RegisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {

    }

    public void UnregisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {

    }

    public string GetWindowTitle(IntPtr handle)
    {
        return "";
    }

    public string GetWindowClassName(IntPtr handle)
    {
        return "";
    }

    public bool IsWindowMaximized(IntPtr handle)
    {
        return false;
    }

    public bool IsWindowMinimized(IntPtr handle)
    {
        return false;
    }

    public bool IsForegroundWindowFullscreen(Screen screen)
    {
        return false;
    }

    public bool IsForegroundWindowMaximized(Screen screen)
    {
        return false;
    }

    public Point GetMousePos()
    {
        return new Point();
    }

    public IntPtr ForegroundWindowHandle { get; } = nint.Zero;

    public int GetWindowPid(IntPtr handle)
    {
        return 0;
    }
}