using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ClassIsland.Core.Controls;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using AppKit;
using ClassIsland.Core;
using Foundation;
using ObjCRuntime;

namespace ClassIsland.Platforms.MacOs.Services;

public class WindowPlatformServices : IWindowPlatformService
{
    private bool _isMacosFxxked = false;
    
    public WindowPlatformServices()
    {
        
        
    }
    
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
        try
        {
            var platformHandle = toplevel.TryGetPlatformHandle();
            var handlePtr = (platformHandle as IMacOSTopLevelPlatformHandle)?.NSWindow ?? IntPtr.Zero;
            if (handlePtr == nint.Zero)
            {
                return;
            }
            var win = Runtime.GetNSObject(handlePtr) as NSWindow;
            if (win == null)
            {
                return;
            }

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
        catch (Exception e)
        {
            if (_isMacosFxxked)
            {
                return;
            }
            _isMacosFxxked = true;
            _ = CommonTaskDialogs.ShowDialog("Fuck macOS Platform Error", e.ToString());
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