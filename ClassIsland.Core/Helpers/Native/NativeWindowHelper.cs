using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using ClassIsland.Core.Models;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using System.IO;
using Avalonia;
using Avalonia.Platform;
using Timer = System.Threading.Timer;

namespace ClassIsland.Core.Helpers.Native;

public static class NativeWindowHelper
{
    public struct WINDOWPOS {
        public nint hwnd;
        public nint hwndInsertAfter;
        public int  x;
        public int  y;
        public int  cx;
        public int  cy;
        public uint flags;
    }

    public static int SWP_NOZORDER = 0x0004;
    public static nint HWND_TOPMOST = -1;
    public static nint HWND_BOTTOM = 1;
    
    public static bool IsOccupied(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch
        {
            return true;
        }
    }

    public static void WaitForFile(string path)
    {
        while (IsOccupied(path))
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.1));
        }
    }

    public static Color GetColor(int argb) =>
        Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)(argb));
    
#if false
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
#endif

}