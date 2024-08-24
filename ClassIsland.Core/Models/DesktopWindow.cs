using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using ClassIsland.Core.Helpers.Native;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models;

public class DesktopWindow : ObservableRecipient
{
    private string _className = "";
    private HWND _hWnd = default;
    private string _windowText = "";
    private Size _windowSize = Size.Empty;
    private BitmapImage _screenShot = new BitmapImage();
    private Process _ownerProcess = Process.GetCurrentProcess();
    private RECT _windowRect = new RECT();
    private BitmapImage _icon = new BitmapImage();
    private bool _isVisible = false;

    public string ClassName
    {
        get => _className;
        set
        {
            if (value == _className) return;
            _className = value;
            OnPropertyChanged();
        }
    }

    public HWND HWnd
    {
        get => _hWnd;
        set
        {
            if (value.Equals(_hWnd)) return;
            _hWnd = value;
            OnPropertyChanged();
        }
    }

    public string WindowText
    {
        get => _windowText;
        set
        {
            if (value == _windowText) return;
            _windowText = value;
            OnPropertyChanged();
        }
    }

    public Size WindowSize
    {
        get => _windowSize;
        set
        {
            if (value.Equals(_windowSize)) return;
            _windowSize = value;
            OnPropertyChanged();
        }
    }

    public BitmapImage ScreenShot
    {
        get => _screenShot;
        set
        {
            if (Equals(value, _screenShot)) return;
            _screenShot = value;
            OnPropertyChanged();
        }
    }

    public Process OwnerProcess
    {
        get => _ownerProcess;
        set
        {
            if (Equals(value, _ownerProcess)) return;
            _ownerProcess = value;
            OnPropertyChanged();
        }
    }

    public RECT WindowRect
    {
        get => _windowRect;
        set
        {
            if (value.Equals(_windowRect)) return;
            _windowRect = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get;
        set;
    } = "";

    public BitmapImage Icon
    {
        get => _icon;
        set
        {
            if (Equals(value, _icon)) return;
            _icon = value;
            OnPropertyChanged();
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (value == _isVisible) return;
            _isVisible = value;
            OnPropertyChanged();
        }
    }

    public static unsafe DesktopWindow GetWindowByHWnd(HWND hWnd)
    {
        var str = NativeWindowHelper.BuildPWSTR(256, out var pstr);
        GetClassName(hWnd, str, 255);
        GetWindowRect((HWND)new HandleRef(null, hWnd).Handle, out RECT rect);
        var window = new DesktopWindow()
        {
            HWnd = hWnd,
            WindowRect = rect,
            ClassName = new string(str),
            IsVisible = IsWindowVisible(hWnd)
        };
        Marshal.FreeHGlobal(pstr);
        return window;
    }

    public static unsafe DesktopWindow GetWindowByHWndDetailed(HWND hWnd)
    {
        var pClassName = NativeWindowHelper.BuildPWSTR(256, out var nClassName);
        GetClassName(hWnd, pClassName, 255);
        var pid = (uint)0;
        GetWindowThreadProcessId(hWnd, &pid);
        var process = Process.GetProcessById((int)pid);
        var pWindowText = NativeWindowHelper.BuildPWSTR(256, out var nWindowText);
        GetWindowText(hWnd, pWindowText, 255);
        GetWindowRect((HWND)new HandleRef(null, hWnd).Handle, out RECT rect);
        var bitmap = BitmapConveters.ConvertToBitmapImage(WindowCaptureHelper.CaptureWindowBitBlt(hWnd),h:300);
        var description = process.MainModule?.FileVersionInfo.FileDescription;
        var windowByHWndDetailed = new DesktopWindow()
        {
            HWnd = hWnd,
            ClassName = new string(pClassName),
            OwnerProcess = process,
            WindowText = new string(pWindowText),
            WindowRect = rect,
            ScreenShot = bitmap,
            Description = (description == "") ? (process.ProcessName) : description ?? process.ProcessName,
            IsVisible = PInvoke.IsWindowVisible(hWnd),
            Icon = BitmapConveters.ConvertToBitmapImage(System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule!.FileName!)!.ToBitmap(), h:32)
        };
        Marshal.FreeHGlobal(nClassName);
        Marshal.FreeHGlobal(nWindowText);
        return windowByHWndDetailed;
    }
}