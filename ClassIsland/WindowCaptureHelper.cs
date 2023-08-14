using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ClassIsland;

/// <summary>
/// Source: https://www.cnblogs.com/kybs0/p/15768990.html
/// </summary>
public class WindowCaptureHelper
{
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
    private static extern IntPtr GetWindowDC(IntPtr hwnd);

    public static Bitmap GetShotCutImage(IntPtr hWnd)
    {
        var hscrdc = GetWindowDC(hWnd);
        var windowRect = new Rectangle();
        GetWindowRect(hWnd, ref windowRect);
        int width = Math.Abs(windowRect.Width - windowRect.X);
        int height = Math.Abs(windowRect.Height - windowRect.Y);
        var hbitmap = CreateCompatibleBitmap(hscrdc, width, height);
        var hmemdc = CreateCompatibleDC(hscrdc);
        SelectObject(hmemdc, hbitmap);
        PrintWindow(hWnd, hmemdc, 0);
        var bmp = Image.FromHbitmap(hbitmap);
        DeleteDC(hscrdc);
        DeleteDC(hmemdc);
        return bmp;
    }
}