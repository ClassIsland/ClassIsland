using System.Runtime.InteropServices;
using Avalonia.Controls;
using static ClassIsland.Platforms.Linux.X;

namespace ClassIsland.Platforms.Linux;

public static class XExtensions
{
    private static IntPtr _atomNetWmState;
    private static IntPtr _atomNetWmStateMaximizedVert;
    private static IntPtr _atomNetWmStateMaximizedHorz;
    private static IntPtr _atomNetWmStateHidden;
    private static IntPtr _atomNetWmStateFullscreen;

    private static nint _display;

    public static void Init(nint display)
    {
        _display = display;
        _atomNetWmState = XInternAtom(display, "_NET_WM_STATE", false);
        _atomNetWmStateMaximizedVert = XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_VERT", false);
        _atomNetWmStateMaximizedHorz = XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_HORZ", false);
        _atomNetWmStateHidden = XInternAtom(display, "_NET_WM_STATE_HIDDEN", false);
        _atomNetWmStateFullscreen = XInternAtom(display, "_NET_WM_STATE_FULLSCREEN", false);

    }
    
    
    public static WindowState GetWindowState(IntPtr window)
    {
        var maxVert = false;
        var maxHorz = false;
        var minimized = false;
        var fullscreen = false;
        IntPtr prop;
        int result = XGetWindowProperty(
            _display, window, _atomNetWmState,
            0, 1024, false, IntPtr.Zero,
            out IntPtr actualType, out int actualFormat,
            out ulong nitems, out ulong bytesAfter, out prop);

        if (result != 0 || prop == IntPtr.Zero || nitems <= 0) 
            return WindowState.Normal;
        // 读取原子列表
        var atoms = new IntPtr[nitems];
        Marshal.Copy(prop, atoms, 0, (int)nitems);

        // 检查状态
        foreach (var atom in atoms)
        {
            if (atom == _atomNetWmStateMaximizedVert)
            {
                maxVert = true;
            }
            else if (atom == _atomNetWmStateMaximizedHorz)
            {
                maxHorz = true;
            }
            else if (atom == _atomNetWmStateHidden)
            {
                minimized = true;
            }
            else if (atom == _atomNetWmStateFullscreen)
            {
                fullscreen = true;
            }
        }
        XFree(prop);

        if (maxHorz && maxVert)
        {
            return WindowState.Maximized;
        }

        if (fullscreen)
        {
            return WindowState.FullScreen;
        }

        if (minimized)
        {
            return WindowState.Minimized;
        }

        return WindowState.Normal;

    }
}