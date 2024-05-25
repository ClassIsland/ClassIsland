using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

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

    public static DesktopWindow GetWindowByHWnd(HWND hWnd)
    {
        var str = new PWSTR();
        GetClassName(hWnd, str, 255);
        GetWindowRect((HWND)new HandleRef(null, hWnd).Handle, out RECT rect);
        return new DesktopWindow()
        {
            HWnd = hWnd,
            WindowRect = rect,
            ClassName = str.ToString(),
            IsVisible = IsWindowVisible(hWnd)
        };
    }

    public unsafe static DesktopWindow GetWindowByHWndDetailed(HWND hWnd)
    {
        var str = new PWSTR();
        GetClassName(hWnd, str, 255);
        uint* pid = default;
        GetWindowThreadProcessId(hWnd, pid);
        var process = Process.GetProcessById((int)*pid);
        var str2 = new PWSTR();
        GetWindowText(hWnd, str2, 255);
        GetWindowRect((HWND)new HandleRef(null, hWnd).Handle, out RECT rect);
        var bitmap = BitmapConveters.ConvertToBitmapImage(WindowCaptureHelper.CaptureWindowBitBlt(hWnd),h:300);
        var description = process.MainModule?.FileVersionInfo.FileDescription;
        return new DesktopWindow()
        {
            HWnd = hWnd,
            ClassName = str.ToString(),
            OwnerProcess = process,
            WindowText = str2.ToString(),
            WindowRect = rect,
            ScreenShot = bitmap,
            Description = (description == "") ? (process.ProcessName) : description ?? process.ProcessName,
            IsVisible = IsWindowVisible(hWnd),
            Icon = BitmapConveters.ConvertToBitmapImage(System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule!.FileName!)!.ToBitmap(), h:32)
        };
    }
}