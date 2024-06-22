using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using ClassIsland.Core.Models;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Helpers.Native;

public static class NativeWindowHelper
{
    #region 常量
    public static readonly IntPtr HFILE_ERROR = new IntPtr(-1);
    public static readonly HWND HWND_BOTTOM = (HWND)new IntPtr(1);

    public const int OF_READWRITE = 2;
    public const int OF_SHARE_DENY_NONE = 0x40;

    public const int WS_EX_DLGMODALFRAME = 0x0001;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TRANSPARENT = 0x20;
    public const int WS_SYSMENU = 0x80000;
    #endregion

    public static bool IsForegroundFullScreen(Screen screen)
    {
        if (screen == null)
        {
            screen = Screen.PrimaryScreen;
        }
        var win = GetForegroundWindow();
        GetWindowRect((HWND)new HandleRef(null, win).Handle, out RECT rect);
        var pClassName = BuildPWSTR(256, out var nClassName);
        GetClassName(win, pClassName, 255);
        //Debug.WriteLine(Process.GetProcessById(pid).ProcessName);
        var className = pClassName.ToString();
        Marshal.FreeHGlobal(nClassName);
        if (className == "WorkerW" || className == "Progman")
        {
            return false;
        }
        return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top).Contains(screen.Bounds);
    }

    public static bool IsForegroundMaxWindow(Screen screen)
    {
        if (screen == null)
        {
            screen = Screen.PrimaryScreen;
        }
        var win = GetForegroundWindow();
        GetWindowRect((HWND)new HandleRef(null, win).Handle, out RECT rect);
        var pClassName = BuildPWSTR(256, out var nClassName);
        GetClassName(win, pClassName, 255);
        var className = pClassName.ToString();
        Marshal.FreeHGlobal(nClassName);
        //Debug.WriteLine(Process.GetProcessById(pid).ProcessName);
        if (className == "WorkerW" || className == "Progman")
        {
            return false;
        }
        return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top).Contains(screen.WorkingArea);
    }

    // 常量
    public static bool IsOccupied(string filePath)
    {
        HANDLE handler = (HANDLE)PInvoke._lopen(filePath, OF_READWRITE | OF_SHARE_DENY_NONE);
        PInvoke.CloseHandle(handler);
        return handler == HFILE_ERROR;
    }

    public static void WaitForFile(string path)
    {
        while (IsOccupied(path))
        {
        }
    }
    
    public static System.Windows.Media.Color GetColor(int argb) => new System.Windows.Media.Color()
    {
        A = (byte)(argb >> 24),
        R = (byte)(argb >> 16),
        G = (byte)(argb >> 8),
        B = (byte)(argb)
    };

    public static IntPtr FindWindowByClass(string className)
    {
        var windows =  GetAllWindows();
        var q = (from i in windows
            where i.ClassName == className
            select i)
            .ToList();
        return q.Count > 0 ? q[0].HWnd : IntPtr.Zero;
    }


    public static List<DesktopWindow> GetAllWindows(bool isDetailed=false)
    {
        var windows = new List<DesktopWindow>();
        string className;
        var queue = new Queue<HWND>();
        queue.Enqueue(HWND.Null);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var win = PInvoke.FindWindowEx(current, HWND.Null, default(PCWSTR), default(PCWSTR));
            while (win != IntPtr.Zero)
            {
                // 检查是否存在子窗口
                var child = PInvoke.FindWindowEx(win, HWND.Null, default(PCWSTR), default(PCWSTR));
                // 获取窗口信息
                try
                {
                    windows.Add(isDetailed
                        ? DesktopWindow.GetWindowByHWndDetailed(win)
                        : DesktopWindow.GetWindowByHWnd(win));
                }
                catch (Exception ex)
                {
                    LoggerExtensions.LogError(IAppHost.GetService<ILogger>(), "无法获取窗口信息：{}", win);
                }

                // 前往下一个窗口
                win = PInvoke.FindWindowEx(current, win, default(PCWSTR), default(PCWSTR));
                if (child == IntPtr.Zero)
                {
                    continue;
                }

                if (win != IntPtr.Zero)
                {
                    queue.Enqueue(win);
                }
            }
        }


        return windows;
    }

    public static unsafe PWSTR BuildPWSTR(int bufferSize, out nint ptr)
    {
        ptr = Marshal.AllocHGlobal(bufferSize * sizeof(char));
        return new PWSTR((char*)ptr.ToPointer());
    }

    public struct StyleStruct
    {
        public int styleOld;
        public int styleNew;
    }
}