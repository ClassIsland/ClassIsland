using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ClassIsland.Core.Controls;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using AppKit;
using ClassIsland.Core;
using CoreGraphics;
using Foundation;
using ObjCRuntime;

namespace ClassIsland.Platforms.MacOs.Services;

public class WindowPlatformServices : IWindowPlatformService
{
    private bool _isMacosFxxked = false;
    
    public WindowPlatformServices()
    {
    }

    private static NSWindow? GetNsWindowFromAvalonia(TopLevel toplevel)
    {
        var platformHandle = toplevel.TryGetPlatformHandle();
        var handlePtr = (platformHandle as IMacOSTopLevelPlatformHandle)?.NSWindow ?? IntPtr.Zero;
        if (handlePtr == nint.Zero)
        {
            return null;
        }
        var win = Runtime.GetNSObject(handlePtr) as NSWindow;
        return win;
    }

    private static NSWindow? GetNsWindowFromHandle(nint handle)
    {
        if (handle == nint.Zero)
        {
            return null;
        }
        return Runtime.GetNSObject(handle) as NSWindow;
    }
    
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
        try
        {
            var win = GetNsWindowFromAvalonia(toplevel);
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
                // Not directly supported on macOS in a simple way.
            }
            if ((features & WindowFeatures.Topmost) > 0)
            {
                win.Level = state ? NSWindowLevel.Floating : NSWindowLevel.Normal;
            }
            if ((features & WindowFeatures.Private) > 0)
            {
                win.SharingType = state ? NSWindowSharingType.None : NSWindowSharingType.ReadOnly;
            }
            if ((features & WindowFeatures.ToolWindow) > 0 && toplevel is Window window)
            {
                if (state)
                {
                    win.StyleMask |= NSWindowStyle.Utility;
                }
                else
                {
                    win.StyleMask &= ~NSWindowStyle.Utility;
                }
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
        var win = GetNsWindowFromHandle(handle);
        return win?.Title ?? "";
    }

    public string GetWindowClassName(IntPtr handle)
    {
        var win = GetNsWindowFromHandle(handle);
        return win?.GetType().FullName ?? "";
    }

    public bool IsWindowMaximized(IntPtr handle)
    {
        var win = GetNsWindowFromHandle(handle);
        return win?.IsZoomed ?? false;
    }

    public bool IsWindowMinimized(IntPtr handle)
    {
        var win = GetNsWindowFromHandle(handle);
        return win?.IsMiniaturized ?? false;
    }

    public bool IsForegroundWindowFullscreen(Screen screen)
    {
        var foregroundWindow = NSApplication.SharedApplication.KeyWindow;
        if (foregroundWindow == null)
        {
            return false;
        }
        return (foregroundWindow.StyleMask & NSWindowStyle.FullScreenWindow) == NSWindowStyle.FullScreenWindow;
    }

    public bool IsForegroundWindowMaximized(Screen screen)
    {
        var foregroundWindow = NSApplication.SharedApplication.KeyWindow;
        return foregroundWindow?.IsZoomed ?? false;
    }

    public Point GetMousePos()
    {
        return new Point();
    }

    public IntPtr ForegroundWindowHandle => NSApplication.SharedApplication.KeyWindow?.Handle ?? nint.Zero;

    public int GetWindowPid(IntPtr handle)
    {
        return 0;
    }
}