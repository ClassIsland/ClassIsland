using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ClassIsland;

/// <summary>
/// Source: https://www.cnblogs.com/kybs0/p/15768990.html
/// </summary>
public static class WindowCaptureHelper
{
    public static uint SRCCOPY = 0x00CC0020;

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rectangle rect);
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")]
    private static extern int DeleteDC(IntPtr hdc);
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool BitBlt(
        IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hObjectSource, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern long GetLastError();
    [DllImport("user32.dll", EntryPoint = "GetWindowDC")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    public static Bitmap GetShotCutImage(IntPtr hWnd)
    {
        var hscrdc = GetWindowDC(hWnd);
        var windowRect = new Rectangle();
        GetWindowRect(hWnd, ref windowRect);
        var width = Math.Abs(windowRect.Width - windowRect.X);
        var height = Math.Abs(windowRect.Height - windowRect.Y);
        var hbitmap = CreateCompatibleBitmap(hscrdc, width, height);
        var hmemdc = CreateCompatibleDC(hscrdc);
        SelectObject(hmemdc, hbitmap);
        PrintWindow(hWnd, hmemdc, 0);
        var bmp = Image.FromHbitmap(hbitmap);
        DeleteDC(hscrdc);
        DeleteDC(hmemdc);
        return bmp;
    }

    public static Bitmap CaptureWindowBitBlt(IntPtr hWnd)
    {
        // 创建兼容内存 DC。
        var wdc = GetWindowDC(hWnd);
        var cdc = CreateCompatibleDC(wdc);

        var windowRect = new Rectangle();
        GetWindowRect(hWnd, ref windowRect);
        var width = Math.Abs(windowRect.Width - windowRect.X);
        var height = Math.Abs(windowRect.Height - windowRect.Y);
        // 创建兼容位图 DC。
        var hBitmap = CreateCompatibleBitmap(wdc, width, height);
        // 关联兼容位图和兼容内存，不这么做，下面的像素位块（bit_block）转换不会生效到 hBitmap。
        var oldHBitmap = SelectObject(cdc, (IntPtr)hBitmap);
        // 注：使用 GDI+ 截取“使用硬件加速过的”应用时，截取到的部分是全黑的。
        var result = BitBlt(cdc, 0, 0, width, height, wdc, 0, 0, SRCCOPY);

        try
        {
            // 保存图片。
            if (result)
            {
                var bmp = Image.FromHbitmap(hBitmap);
                return bmp;
            }
            else
            {
                var error = GetLastError();
                throw new Win32Exception((int)error);
            }
        }
        finally
        {
            // 回收资源。
            SelectObject(cdc, oldHBitmap);
            DeleteObject((IntPtr)hBitmap);
            DeleteDC(cdc);
            ReleaseDC(hWnd, wdc);
        }
    }
}