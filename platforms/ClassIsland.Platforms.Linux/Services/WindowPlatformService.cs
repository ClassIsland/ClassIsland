using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
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
    
    public delegate int XErrorHandlerDelegate(IntPtr display, IntPtr errorEvent);

    private static CancellationToken _stopToken;

    
    private static int XErrorHandler(IntPtr display, IntPtr errorEvent)
    {
        if (display != self?._display)
        {
            return 0;
        }
        Console.WriteLine("X Error occurred");
        display = XOpenDisplay(null);
        return 0;
    }

    private static Delegate _xErrorHandlerDelegate = new XErrorHandlerDelegate(XErrorHandler);
    
    public WindowPlatformService(CancellationToken stopToken)
    {
        _stopToken = stopToken;
        
        XInitThreads();
        
        _display = XOpenDisplay(null);
        self = this;
        
        XExtensions.Init(_display);
        
        XSetErrorHandler(Marshal.GetFunctionPointerForDelegate(_xErrorHandlerDelegate));
        _root = XDefaultRootWindow(_display);
        _atomActiveWindow = XInternAtom(_display, "_NET_ACTIVE_WINDOW", false);
        
    }

    ~WindowPlatformService()
    {
        XCloseDisplay(_display);
    }

    public void PostInit()
    {
        new Task(XEventLoop, _stopToken).Start();
    }
    
    private static void XEventLoop()
    {
        XInitThreads();
        
        var display = XOpenDisplay(null);
        var root = XDefaultRootWindow(display);
        var atomActiveWindow = XInternAtom(display, "_NET_ACTIVE_WINDOW", false);
        XSelectInput(display, root, XEventMask.PropertyChangeMask);
        try
        {
            while (!_stopToken.IsCancellationRequested)
            {
                var status  = XNextEvent(display, out var ev);
                if (status != 0)
                {
                    continue;
                }

                if (ev.type != XEventName.PropertyNotify /* PropertyNotify */)
                    continue;
                var atom = atomActiveWindow;
                IntPtr actualType;
                int actualFormat;
                ulong nitems, bytesAfter;
                IntPtr propPtr;
                var result = XGetWindowProperty(
                    display,
                    root,
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
        
        if ((features & WindowFeatures.Transparent) > 0 && state)
        {
            var region = XFixesCreateRegion(_display, 0, 0);
            XFixesSetWindowShapeRegion(_display, handle, ShapeInput, 0, 0, region);
        }
        if ((features & WindowFeatures.Bottommost) > 0)
        {
            XLowerWindow(_display, handle);
        }
        if ((features & WindowFeatures.Topmost) > 0)
        {
            XRaiseWindow(_display, handle);
            var netWmState = XInternAtom(_display, "_NET_WM_STATE", false);
            var netWmStateAbove = XInternAtom(_display, "_NET_WM_STATE_ABOVE", false);
            nint[] atoms = { netWmStateAbove };
            XChangeProperty(
                _display, handle,
                netWmState,             // property
                4,                // type
                32,                     // format: 32-bit
                0,        // 替换模式
                atoms, atoms.Length
            );
        }
        if ((features & WindowFeatures.Private) > 0)
        {
            
        }
        if ((features & WindowFeatures.ToolWindow) > 0 && toplevel is Window window)
        {
            X11Properties.SetNetWmWindowType(window, state ? X11NetWmWindowType.Utility : X11NetWmWindowType.Normal);
        }
        if ((features & WindowFeatures.SkipManagement) > 0)
        {
            var attributes = new XSetWindowAttributes();
            attributes.override_redirect = state ? 1 : 0;
            XChangeWindowAttributes(_display, handle, CWOverrideRedirect, ref attributes);
            XMapWindow(_display, handle);
            XFlush(_display);
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
        XTextProperty textProp;
        var atomNetName = XInternAtom(_display, "_NET_WM_NAME", false);
        if (XGetTextProperty(_display, handle, out textProp, atomNetName) == 0)
        {
            IntPtr atomWmName = XInternAtom(_display, "WM_NAME", false);
            XGetTextProperty(_display, handle, out textProp, atomWmName);
        }
        var title = textProp.value != IntPtr.Zero 
            ? Marshal.PtrToStringAnsi(textProp.value) ?? "" 
            : "";

        return title;
    }

    public string GetWindowClassName(IntPtr handle)
    {
        if (XGetClassHint(_display, handle, out var hint) == 0)
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
        var root = _root;

        var success = XQueryPointer(
            _display,
            root,
            out _,
            out _,
            out var rootX,
            out var rootY,
            out _,
            out _,
            out _);

        return new Point(rootX, rootY);
    }

    public IntPtr ForegroundWindowHandle { get; set; }
    public int GetWindowPid(IntPtr handle)
    {
        IntPtr pidAtom       = XInternAtom(_display, "_NET_WM_PID",    false);
        IntPtr cardinalAtom  = XInternAtom(_display, "CARDINAL",       false);
        if (pidAtom == IntPtr.Zero || cardinalAtom == IntPtr.Zero)
            return -1;

        IntPtr actualType;
        int    actualFormat;
        ulong  nItems, bytesAfter;
        IntPtr propPid;

        int status = XGetWindowProperty(
            _display,
            handle,
            pidAtom,
            0,      // long_offset
            1,      // long_length
            false,  // delete
            cardinalAtom,
            out actualType,
            out actualFormat,
            out nItems,
            out bytesAfter,
            out propPid);

        if (status != 0 || propPid == IntPtr.Zero || nItems == 0)
            return 0;

        // 读取第一个（也是唯一一个）32 位整数
        int pid = Marshal.ReadInt32(propPid);
        XFree(propPid);
        return pid;
    }

    public void ClearWindow(TopLevel topLevel)
    {
        var handle = topLevel.TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero)
        {
            return;
        }

        XClearWindow(_display, handle);
    }
}