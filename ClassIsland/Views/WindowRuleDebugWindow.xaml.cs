using System;
using System.Diagnostics;
using System.Windows;
using Windows.Win32.UI.Accessibility;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.ViewModels;
using ClassIsland.Core.Helpers.Native;
using System.Windows.Forms;

namespace ClassIsland.Views;

/// <summary>
/// WindowRuleDebugWindow.xaml 的交互逻辑
/// </summary>
public partial class WindowRuleDebugWindow
{
    public IWindowRuleService WindowRuleService { get; }

    public WindowRuleDebugViewModel ViewModel { get; } = new();

    public WindowRuleDebugWindow(IWindowRuleService windowRuleService)
    {
        WindowRuleService = windowRuleService;
        InitializeComponent();
        DataContext = this;
    }

    private void WindowRuleDebugWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowRuleService.ForegroundWindowChanged += WindowRuleServiceOnForegroundWindowChanged;
        UpdateInfo();
    }

    private void WindowRuleServiceOnForegroundWindowChanged(HWINEVENTHOOK hwineventhook, uint @event, HWND hwnd, int idobject, int idchild, uint ideventthread, uint dwmseventtime)
    {
        UpdateInfo();
    }

    private void WindowRuleDebugWindow_OnUnloaded(object sender, RoutedEventArgs e)
    {
        WindowRuleService.ForegroundWindowChanged -= WindowRuleServiceOnForegroundWindowChanged;
    }

    private unsafe void UpdateInfo()
    {
        var hWnd = GetForegroundWindow();
        ViewModel.ForegroundWindowHandle = Convert.ToString(hWnd);
        try
        {
            const int nMaxCount = 256;
            using var className = new DisposablePWSTR(nMaxCount);
            using var title = new DisposablePWSTR(nMaxCount);
            uint pid = 0;

            ViewModel.ForegroundWindowClassName =
                GetClassName(hWnd, className.PWSTR, nMaxCount) != 0 ? className.ToString() : "";
            ViewModel.ForegroundWindowTitle =
                GetWindowText(hWnd, title.PWSTR, nMaxCount) != 0 ? title.ToString() : "";
            UpdateWindowState(hWnd);

            if (GetWindowThreadProcessId(hWnd, &pid) != 0)
            {
                var process = Process.GetProcessById((int)pid);
                ViewModel.ForegroundWindowProcessName = process.ProcessName;
            }
            else
            {
                ViewModel.ForegroundWindowProcessName = "？？？";
            }

        }
        catch (Exception e)
        {
            // ignored
        }
    }

    private void UpdateWindowState(HWND hWnd)
    {
        GetWindowRect(hWnd, out var rect);
        var mw = App.GetService<MainWindow>();
        var screen = mw.ViewModel.Settings.WindowDockingMonitorIndex < Screen.AllScreens.Length &&
                     mw.ViewModel.Settings.WindowDockingMonitorIndex >= 0 ?
            Screen.AllScreens[mw.ViewModel.Settings.WindowDockingMonitorIndex] : Screen.PrimaryScreen;
        if (screen == null)
        {
            return;
        }

        var fullscreen = NativeWindowHelper.IsForegroundFullScreen(screen);
        var maximize = IsZoomed(hWnd);
        var minimize = IsIconic(hWnd);

        if (fullscreen)
        {
            ViewModel.ForegroundWindowState = "全屏";
            return;
        }
        if (maximize)
        {
            ViewModel.ForegroundWindowState = "最大化";
            return;
        }
        if (minimize)
        {
            ViewModel.ForegroundWindowState = "最小化";
            return;
        }
        ViewModel.ForegroundWindowState = "正常";
    }
}