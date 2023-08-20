using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using static ClassIsland.NativeWindowHelper;

namespace ClassIsland.Models;

public class DesktopWindow : ObservableRecipient
{
    private string _className = "";
    private IntPtr _hWnd = IntPtr.Zero;
    private string _windowText = "";
    private Size _windowSize = Size.Empty;
    private BitmapImage _screenShot = new BitmapImage();
    private Process _ownerProcess = Process.GetCurrentProcess();
    private RECT _windowRect = new RECT();

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

    public IntPtr HWnd
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

    public static DesktopWindow GetWindowByHWnd(IntPtr hWnd)
    {
        var sb = new StringBuilder(255);
        GetClassName(hWnd, sb, 255);
        RECT rect = new RECT();
        GetWindowRect(new HandleRef(null, hWnd).Handle, ref rect);
        return new DesktopWindow()
        {
            HWnd = hWnd,
            WindowRect = rect,
            ClassName = sb.ToString()
        };
    }

    public static DesktopWindow GetWindowByHWndDetailed(IntPtr hWnd)
    {
        var sb = new StringBuilder(255);
        GetClassName(hWnd, sb, 255);
        GetWindowThreadProcessId(hWnd, out var pid);
        var process = Process.GetProcessById(pid);
        var sb2 = new StringBuilder(255);
        GetWindowText(hWnd, sb2, 255);
        RECT rect = new RECT();
        GetWindowRect(new HandleRef(null, hWnd).Handle, ref rect);
        var bitmap = BitmapConveters.ConvertToBitmapImage(WindowCaptureHelper.CaptureWindowBitBlt(hWnd),h:300);
        var description = process.MainModule?.FileVersionInfo.FileDescription;
        return new DesktopWindow()
        {
            HWnd = hWnd,
            ClassName = sb.ToString(),
            OwnerProcess = process,
            WindowText = sb2.ToString(),
            WindowRect = rect,
            ScreenShot = bitmap,
            Description = (description == "") ? (process.ProcessName) : description ?? process.ProcessName
        };
    }
}