using System.Runtime.InteropServices;
using System;
using System.Drawing;
using static HandyControl.Tools.Interop.InteropValues;
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
    

    public static bool IsForegroundFullScreen(Screen screen)
    {
        if (screen == null)
        {
            screen = Screen.PrimaryScreen;
        }
        RECT rect = new RECT();
        GetWindowRect(new HandleRef(null, GetForegroundWindow()), ref rect);
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top).Contains(screen.Bounds);
    }
}