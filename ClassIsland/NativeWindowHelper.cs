using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ClassIsland;

public static class NativeWindowHelper
{
    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(string lp1, string lp2);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public const int WS_EX_TRANSPARENT = 0x20;

    public const int GWL_EXSTYLE = -20;

    [DllImport("user32", EntryPoint = "SetWindowLong")]
    public static extern uint SetWindowLong(IntPtr hwnd, int nIndex, long dwNewLong);

    [DllImport("user32", EntryPoint = "GetWindowLong")]
    public static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

    public struct POINT
    {
        public int X;
        public int Y;
        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    /// <summary>   
    /// 获取鼠标的坐标   
    /// </summary>   
    /// <param name="lpPoint">传址参数，坐标point类型</param>   
    /// <returns>获取成功返回真</returns>   
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetCursorPos(out POINT pt);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public const UInt32 SWP_NOSIZE = 0x0001;
    public const UInt32 SWP_NOMOVE = 0x0002;
    public const UInt32 SWP_NOACTIVATE = 0x0010;
    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

    public const int GWL_STYLE = -16;
    public const int WS_SYSMENU = 0x80000;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(
        [In] IntPtr hWnd,
        out int id
    );


    public static bool IsForegroundFullScreen(Screen screen)
    {
        if (screen == null)
        {
            screen = Screen.PrimaryScreen;
        }
        RECT rect = new RECT();
        var win = GetForegroundWindow();
        GetWindowRect(new HandleRef(null, win), ref rect);
        GetWindowThreadProcessId(win, out var pid);
        try
        {
            //Debug.WriteLine(Process.GetProcessById(pid).ProcessName);
            if (Process.GetProcessById(pid).ProcessName == "explorer")
            {
                return false;
            }
        }
        catch
        {
            // ignored
        }
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top).Contains(screen.Bounds);
    }

    public static bool IsForegroundMaxWindow(Screen screen)
    {
        if (screen == null)
        {
            screen = Screen.PrimaryScreen;
        }
        RECT rect = new RECT();
        var win = GetForegroundWindow();
        GetWindowRect(new HandleRef(null, win), ref rect);
        GetWindowThreadProcessId(win, out var pid);
        //Debug.WriteLine(Process.GetProcessById(pid).ProcessName);
        if (Process.GetProcessById(pid).ProcessName == "explorer")
        {
            return false;
        }
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top).Contains(screen.WorkingArea);
    }
    
    // 判断文件是否打开
    [DllImport("kernel32.dll")]
    public static extern IntPtr _lopen(string lpPathName, int iReadWrite);

    // 关闭文件句柄
    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    // 常量
    public const int OF_READWRITE = 2;
    public const int OF_SHARE_DENY_NONE = 0x40;
    public static readonly IntPtr HFILE_ERROR = new IntPtr(-1);
    public static bool IsOccupied(string filePath)
    {
        IntPtr handler = _lopen(filePath, OF_READWRITE | OF_SHARE_DENY_NONE);
        CloseHandle(handler);
        return handler == HFILE_ERROR;
    }

    public static void WaitForFile(string path)
    {
        while (IsOccupied(path))
        {
        }
    }
}