using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Accessibility;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core.Controls;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platform.Windows.Services;

public class WindowPlatformService : IWindowPlatformService, IDisposable
{
    private const int PwstrCapcity = 256;

    private static List<HWINEVENTHOOK> _hooks = [];
    private static bool _hooksUnhooked = false;
    private static readonly object _hookLock = new();

    private static WINEVENTPROC? _eventProc;
    
    private List<EventHandler<ForegroundWindowChangedEventArgs>> _changedEventHandlers = [];

    private bool _isMoving = false;

    public nint ForegroundWindowHandle { get; set; } = nint.Zero;

    public void Dispose()
    {
        lock (_hookLock)
        {
            if (_hooksUnhooked)
                return;

            for (int i = 0; i < _hooks.Count; i++)
            {
                var hook = _hooks[i];
                if (hook != default)
                {
                    UnhookWinEvent(hook);
                }
                _hooks[i] = default;
            }
            _hooks.Clear();
            _hooksUnhooked = true;
            _eventProc = null;
            GC.SuppressFinalize(this);
        }
    }
    
    private void InitEventHook()
    {
        lock (_hookLock) 
        {
            if (_hooksUnhooked) 
            {
                return;
            }
        }
        _eventProc = PfnWinEventProc;
        uint[] events = [EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_MOVESIZEEND, EVENT_SYSTEM_MOVESIZESTART,
            EVENT_SYSTEM_MINIMIZEEND, EVENT_OBJECT_LOCATIONCHANGE];
        foreach (var i in events)
        {
            var flags = WINEVENT_OUTOFCONTEXT;
            if (i == EVENT_OBJECT_LOCATIONCHANGE)
            {
                flags |= WINEVENT_SKIPOWNPROCESS;
            }
            var hook = SetWinEventHook(
                i, i,
                HMODULE.Null, _eventProc,
                0, 0,
                flags);
            _hooks.Add(hook);
        }
    }
    
    private void PfnWinEventProc(HWINEVENTHOOK hook, uint @event, HWND hwnd, int idObject, int child, uint thread, uint time)
    {
        if (hwnd == HWND.Null || new HandleRef(null, hwnd).Handle == IntPtr.Zero)
        {
            return;
        }

        var currentForeground = GetForegroundWindow();
        if (currentForeground == HWND.Null || new HandleRef(null, currentForeground).Handle == IntPtr.Zero)
        {
            return;
        }

        if (hwnd != currentForeground)
        {
            return;
        }

        if (@event == EVENT_OBJECT_LOCATIONCHANGE && _isMoving)
        {
            return;
        }

        _isMoving = @event switch
        {
            EVENT_SYSTEM_MOVESIZESTART => true,
            EVENT_SYSTEM_MOVESIZEEND => false,
            _ => _isMoving
        };

        var foregroundWindow = ForegroundWindowHandle = currentForeground;
        Dispatcher.UIThread.Invoke(() =>
        {
            foreach (var handler in _changedEventHandlers)
            {
                handler.Invoke(this, new ForegroundWindowChangedEventArgs(foregroundWindow));
            }
        });
    }
    
    public void SetWindowFeature(TopLevel toplevel, WindowFeatures features, bool state)
    {
        var handle = toplevel.TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero)
        {
            return;
        }

        if ((features & WindowFeatures.Transparent) > 0)
        {
            var style = GetWindowLong((HWND)handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            if (state)
            {
                style |= (int)WINDOW_EX_STYLE.WS_EX_LAYERED | (int)WINDOW_EX_STYLE.WS_EX_TRANSPARENT;
                SetLayeredWindowAttributes((HWND)handle, new COLORREF(0), 255, (LAYERED_WINDOW_ATTRIBUTES_FLAGS)0x2);
            }
            else
            {
                style &= ~(int)WINDOW_EX_STYLE.WS_EX_TRANSPARENT;
            }
            var r = SetWindowLong((HWND)handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);
        }
        if ((features & WindowFeatures.Bottommost) > 0)
        {
            if (state)
            {
                SetWindowPos((HWND)handle, HWND.HWND_BOTTOM, 0, 0, 0, 0,
                    SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSENDCHANGING);
            }
        }
        if ((features & WindowFeatures.Topmost) > 0)
        {
            if (state)
            {
                SetWindowPos((HWND)handle, HWND.HWND_TOPMOST, 0, 0, 0, 0,
                    SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOSENDCHANGING);
            }
        }
        if ((features & WindowFeatures.Private) > 0)
        {
            SetWindowDisplayAffinity((HWND)handle, state ? WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE : WINDOW_DISPLAY_AFFINITY.WDA_NONE);
        }
        if ((features & WindowFeatures.ToolWindow) > 0)
        {
            var style = GetWindowLong((HWND)handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            if (state)
            {
                style |= (int)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
            }
            else
            {
                style &= ~(int)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
            }
            var r = SetWindowLong((HWND)handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);
        }
        
    }

    public WindowFeatures GetWindowFeatures(TopLevel topLevel)
    {
        return (WindowFeatures)0;
    }

    public void RegisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {
        if (_hooks.Count <= 0 && !_hooksUnhooked)
        {
            InitEventHook();
        }
        _changedEventHandlers.Add(handler);
    }

    public void UnregisterForegroundWindowChangedEvent(EventHandler<ForegroundWindowChangedEventArgs> handler)
    {
        _changedEventHandlers.Remove(handler);
    }

    public string GetWindowTitle(IntPtr handle)
    {
        var pClassName = NativeHelpers.BuildPWSTR(PwstrCapcity, out var nClassName);
        try
        {
            _ = GetWindowText((HWND)handle, pClassName, PwstrCapcity - 1);
            var className = pClassName.ToString();
            return className;
        }
        finally
        {
            Marshal.FreeHGlobal(nClassName);
        }
    }

    public string GetWindowClassName(IntPtr handle)
    {
        var pClassName = NativeHelpers.BuildPWSTR(PwstrCapcity, out var nClassName);
        try
        {
            _ = GetClassName((HWND)handle, pClassName, PwstrCapcity - 1);
            var className = pClassName.ToString();
            return className;
        }
        finally
        {
            Marshal.FreeHGlobal(nClassName);
        }
    }

    public bool IsWindowMaximized(IntPtr handle)
    {
        return IsZoomed((HWND)handle);
    }

    public bool IsWindowMinimized(IntPtr handle)
    {
        return IsIconic((HWND)handle);
    }

    public bool IsForegroundWindowFullscreen(Screen screen)
    {
        var win = GetForegroundWindow();
        if (win == HWND.Null || new HandleRef(null, win).Handle == IntPtr.Zero) 
        {
            return false;
        }
        GetWindowRect((HWND)new HandleRef(null, win).Handle, out RECT rect);
        var pClassName = NativeHelpers.BuildPWSTR(PwstrCapcity, out var nClassName);
        try
        {
            GetClassName(win, pClassName, PwstrCapcity  - 1);
            var className = pClassName.ToString();
            if (className is "WorkerW" or "Progman")
            {
                return false;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(nClassName);
        }
        return new PixelRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top).Contains(screen.Bounds);

    }

    public bool IsForegroundWindowMaximized(Screen screen)
    {
        var win = GetForegroundWindow();
        if (win == HWND.Null || new HandleRef(null, win).Handle == IntPtr.Zero) 
        {
            return false;
        }
        GetWindowRect((HWND)new HandleRef(null, win).Handle, out RECT rect);
        var pClassName = NativeHelpers.BuildPWSTR(PwstrCapcity, out var nClassName);
        try
        {
            GetClassName(win, pClassName, PwstrCapcity - 1);
            var className = pClassName.ToString();
            //Debug.WriteLine(Process.GetProcessById(pid).ProcessName);
            if (className is "WorkerW" or "Progman")
            {
                return false;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(nClassName);
        }
        return new PixelRect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top).Contains(screen.WorkingArea);
    }

    public Point GetMousePos()
    {
        GetCursorPos(out var ptr);
        return new Point(ptr.X, ptr.Y);
    }
    
    public unsafe int GetWindowPid(IntPtr handle)
    {
        uint pid = 0;
        _ = GetWindowThreadProcessId((HWND)handle, &pid);
        return (int)pid;
    }

    public void ClearWindow(TopLevel topLevel)
    {
        
    }
}