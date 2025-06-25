using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using static ClassIsland.Platforms.Linux.X;

namespace ClassIsland.Platforms.Linux.Services;

public class WindowPlatformService : IWindowPlatformService
{
    private List<EventHandler<ForegroundWindowChangedEventArgs>> _handlers = [];
    
    private nint _display;
    private nint _root;
    private nint _atomActiveWindow;

    private static WindowPlatformService self = null!;
    
    public WindowPlatformService()
    {
        // XInitThreads();
        
        _display = XOpenDisplay(null);
        self = this;
        
        XExtensions.Init(_display);
        
        _root = XDefaultRootWindow(_display);
        _atomActiveWindow = XInternAtom(_display, "_NET_ACTIVE_WINDOW", false);

        // 订阅根窗口的 PropertyNotify 事件
        XSelectInput(_display, _root, (1 << 22));
    }

    public void PostInit()
    {
        new Thread(XEventLoop).Start();
    }
    
    private static void XEventLoop()
    {
        try
        {
            while (true)
            {
                var status  = XNextEvent(self._display, out var ev);
                if (status != 0)
                {
                    continue;
                }

                if (ev.type != XEventName.PropertyNotify /* PropertyNotify */)
                    continue;
                var atom = self._atomActiveWindow;
                IntPtr actualType;
                int actualFormat;
                ulong nitems, bytesAfter;
                IntPtr propPtr;
                var result = XGetWindowProperty(
                    self._display,
                    self._root,
                    atom,
                    0,
                    1,
                    false,
                    nint.Zero,
                    out actualType,
                    out actualFormat,
                    out nitems,
                    out bytesAfter,
                    out propPtr
                );
                
                var winId = Marshal.ReadIntPtr(propPtr);
                XFree(propPtr);
                
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    self.ForegroundWindowHandle = winId;
                    foreach (var handler in self._handlers)
                    {
                        handler.Invoke(self, new ForegroundWindowChangedEventArgs(winId));
                    }
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
        var handle = toplevel.TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero)
        {
            return;
        }

        var display = XOpenDisplay(null);
        
        if ((features & WindowFeatures.Transparent) > 0)
        {
            var region = XFixesCreateRegion(display, 0, 0);
            XFixesSetWindowShapeRegion(display, handle, ShapeInput, 0, 0, region);
        }
        if ((features & WindowFeatures.Bottommost) > 0)
        {
            XLowerWindow(display, handle);
        }
        if ((features & WindowFeatures.Topmost) > 0)
        {
            XRaiseWindow(display, handle);
        }
        if ((features & WindowFeatures.Private) > 0)
        {
            
        }
        if ((features & WindowFeatures.ToolWindow) > 0)
        {
            
        }

        XCloseDisplay(display);
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
        var display = XOpenDisplay(null);
        XTextProperty textProp;
        var atomNetName = XInternAtom(display, "_NET_WM_NAME", false);
        if (XGetTextProperty(display, handle, out textProp, atomNetName) == 0)
        {
            IntPtr atomWmName = XInternAtom(display, "WM_NAME", false);
            XGetTextProperty(display, handle, out textProp, atomWmName);
        }
        var title = textProp.value != IntPtr.Zero 
            ? Marshal.PtrToStringAnsi(textProp.value) ?? "" 
            : "";

        return title;
    }

    public string GetWindowClassName(IntPtr handle)
    {
        var display = XOpenDisplay(null);
        if (XGetClassHint(display, handle, out var hint) == 0)
            throw new Exception("无法获取 ClassHint");

        var cls   = Marshal.PtrToStringAnsi(hint.res_class);

        XFree(hint.res_class);

        return cls ?? "";
    }

    public bool IsWindowMaximized(IntPtr handle)
    {
        return XExtensions.GetWindowState(handle) == WindowState.Maximized;
    }

    public bool IsWindowMinimized(IntPtr handle)
    {
        return XExtensions.GetWindowState(handle) == WindowState.Minimized;
    }

    public bool IsForegroundWindowFullscreen(Screen screen)
    {  
        return XExtensions.GetWindowState(ForegroundWindowHandle) == WindowState.FullScreen;
    }

    public bool IsForegroundWindowMaximized(Screen screen)
    {
        return XExtensions.GetWindowState(ForegroundWindowHandle) == WindowState.Maximized;
    }

    public Point GetMousePos()
    {
        var display = XOpenDisplay(null);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("无法打开 X11 显示");
        
        var root = _root;

        var success = XQueryPointer(
            display,
            root,
            out _,
            out _,
            out var rootX,
            out var rootY,
            out _,
            out _,
            out _);

        XCloseDisplay(display);

        return new Point(rootX, rootY);
    }

    public IntPtr ForegroundWindowHandle { get; set; }
    public int GetWindowPid(IntPtr handle)
    {
        return 0;
    }
    
    
}