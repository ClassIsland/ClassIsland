using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using ClassIsland.Core.Controls;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Platforms.MacOs.Models;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using ObjCRuntime;

namespace ClassIsland.Platforms.MacOs.Services;

public class WindowPlatformServices : IWindowPlatformService, IDisposable
{
    private bool _isMacosFxxked = false;
    private List<EventHandler<ForegroundWindowChangedEventArgs>> _handlers = [];
    private NSObject? _observer;

    private List<WindowInfo> _cachedWindowList = [];
    
    public WindowPlatformServices()
    {
        NSApplication.Init();
    }

    public object? GetWindowFromHandle(nint handle)
    {
        // This method can only get windows of the current application.
        return NSApplication.SharedApplication.WindowWithWindowNumber(handle);
    }

    private nint GetForegroundWindow(NSRunningApplication app)
    {
        // 获取应用的进程ID
        var targetPid = app.ProcessIdentifier;
        
        // 获取所有屏幕上的窗口信息
        var windowList = API.CGWindowListCopyWindowInfo(
            CGWindowListOption.OnScreenOnly | CGWindowListOption.ExcludeDesktopElements, 0);
        
        if (Runtime.GetNSObject(windowList) is not NSArray windows){
            API.CFRelease(windowList);
            return nint.Zero;
        }
        IntPtr frontmostWindow = IntPtr.Zero;
        int highestLevel = int.MinValue;
        _cachedWindowList = windows.ToArray().Select(x =>
        {
            var winId = x.ValueForKey(new NSString("kCGWindowNumber")) as NSNumber;
            var pid = x.ValueForKey(new NSString("kCGWindowOwnerPID")) as NSNumber;
            var processName = x.ValueForKey(new NSString("kCGWindowOwnerName")) as NSString;
            var windowName = x.ValueForKey(new NSString("kCGWindowName")) as NSString;
            var boundsDict = x.ValueForKey(new NSString("kCGWindowBounds")) as NSDictionary;
            CGRect rect = CGRect.Empty;
            API.CGRectMakeWithDictionaryRepresentation(boundsDict.GetHandle(), ref rect);
            var windowLevel = x.ValueForKey(new NSString("kCGWindowLayer")) as NSNumber;
            return new WindowInfo(winId?.Int64Value ?? 0, processName?.ToString() ?? "", pid?.Int32Value ?? 0, windowName?.ToString() ?? "", rect, windowLevel?.Int32Value ?? 0);
        }).ToList();
        API.CFRelease(windowList);
        
        foreach (var windowInfo in _cachedWindowList)
        {
            // 检查窗口所属的进程ID是否匹配
            if (windowInfo.Pid == targetPid)
            {
                var level = windowInfo.WindowLevel;
                
                // 找到层级最高的窗口
                if (level > highestLevel)
                {
                    highestLevel = level;
                    frontmostWindow = new nint(windowInfo.WinId);
                }
            }
        }
        
        return frontmostWindow;
    }

    public void PostInit()
    {
        _observer = NSWorkspace.Notifications.ObserveDidActivateApplication((sender, args) =>
        {
            var app = args.Application;
            _foregroundPid = args.Application.ProcessIdentifier;
            var window = GetForegroundWindow(app);
            ForegroundWindowHandle = window;
            foreach (var handler in _handlers)
            {
                handler.Invoke(this, new ForegroundWindowChangedEventArgs(ForegroundWindowHandle));
            }
            IAppHost.TryGetService<ILogger<WindowPlatformServices>>()?.LogDebug("window changed!!!!!!");
        });
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
        return new NSWindow(handle);
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
                win.Level = state ? NSWindowLevel.ScreenSaver : NSWindowLevel.Normal;
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

            // 仅macOS处理隐藏标题栏特性
            // Fuck macOS, this is a hacky way to hide the title bar.
            // Fuck Copilot, brings me more error messages.
            if ((features & WindowFeatures.HiddenTitleBar) > 0 && toplevel is Window macWindow)
            {
#if MACOS
                // 通过反射获取Handle字段
                var platformImpl = macWindow.GetType().GetProperty("PlatformImpl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.GetValue(macWindow);
                var handleProp = platformImpl?.GetType().GetProperty("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                var handleObj = handleProp?.GetValue(platformImpl);
                var rawHandle = handleObj?.GetType().GetProperty("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.GetValue(handleObj);
                var handle = rawHandle is IntPtr ptr ? ptr : IntPtr.Zero;
                if (handle != IntPtr.Zero)
                {
                    // P/Invoke声明
                    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
                    static extern IntPtr sel_registerName(string name);
                    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
                    static extern void objc_msgSend_void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);
                    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
                    static extern void objc_msgSend_void_bool(IntPtr receiver, IntPtr selector, bool arg);
                    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
                    static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

                    // 1. titleVisibility = .hidden
                    var selTitleVisibility = sel_registerName("setTitleVisibility:");
                    var NSWindowTitleHidden = (IntPtr)1;
                    objc_msgSend_void_IntPtr(handle, selTitleVisibility, NSWindowTitleHidden);

                    // 2. titlebarAppearsTransparent = true
                    var selTitlebarAppearsTransparent = sel_registerName("setTitlebarAppearsTransparent:");
                    objc_msgSend_void_bool(handle, selTitlebarAppearsTransparent, true);

                    // 3. styleMask.insert(.fullSizeContentView)
                    var selStyleMask = sel_registerName("styleMask");
                    var styleMask = objc_msgSend_IntPtr(handle, selStyleMask);
                    var NSWindowStyleMaskFullSizeContentView = (IntPtr)(1 << 15);
                    styleMask = (IntPtr)(styleMask.ToInt64() | NSWindowStyleMaskFullSizeContentView.ToInt64());
                    var selSetStyleMask = sel_registerName("setStyleMask:");
                    objc_msgSend_void_IntPtr(handle, selSetStyleMask, styleMask);
                }
#endif
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
        _handlers.Add(handler);
    }

    public void UnregisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {
        _handlers.Remove(handler);
    }

    public string GetWindowTitle(IntPtr handle)
    {
        return _cachedWindowList.FirstOrDefault(x => x.WinId == handle)?.Title ?? "";
    }

    public string GetWindowClassName(IntPtr handle)
    {
        // Unsupported
        return "";
    }

    public bool IsWindowMaximized(IntPtr handle)
    {
        // Unsupported
        return false;
    }

    public bool IsWindowMinimized(IntPtr handle)
    {
        // Unsupported
        return false;
    }

    public bool IsForegroundWindowFullscreen(Screen screen)
    {
        var foreground = _cachedWindowList
            .FirstOrDefault(x => x.WinId == ForegroundWindowHandle);
        if (foreground == null)
        {
            return false;
        }
        var fgRect = new PixelRect((int)foreground.Bounds.X.Value, (int)foreground.Bounds.Y.Value, (int)foreground.Bounds.Width.Value, (int)foreground.Bounds.Height.Value);        
        return fgRect.Contains(screen.Bounds);
    }

    public bool IsForegroundWindowMaximized(Screen screen)
    {
        var foreground = _cachedWindowList
            .FirstOrDefault(x => x.WinId == ForegroundWindowHandle);
        if (foreground == null)
        {
            return false;
        }
        var fgRect = new PixelRect((int)foreground.Bounds.X.Value, (int)foreground.Bounds.Y.Value, (int)foreground.Bounds.Width.Value, (int)foreground.Bounds.Height.Value);        
        return fgRect.Contains(screen.WorkingArea);
    }

    public Point GetMousePos()
    {
        // Unsupported
        return new Point();
    }

    public IntPtr ForegroundWindowHandle { get; set; } = nint.Zero;

    private int _foregroundPid = 0;

    public int GetWindowPid(IntPtr handle)
    {
        if (handle != ForegroundWindowHandle)
        {
            return 0;
        }
        return _foregroundPid; // Cannot get PID from NSWindow handle directly in a simple way.
    }

    public void Dispose()
    {
        if (_observer != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_observer);
            _observer = null;
        }
    }
    
    public void ClearWindow(TopLevel topLevel)
    {
        
    }
}