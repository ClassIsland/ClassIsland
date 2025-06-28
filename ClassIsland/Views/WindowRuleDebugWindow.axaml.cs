using System;
using System.Diagnostics;
using System.Windows;
using Avalonia.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Platform;
using ClassIsland.Core.Controls;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Views;

/// <summary>
/// WindowRuleDebugWindow.xaml 的交互逻辑
/// </summary>
public partial class WindowRuleDebugWindow : MyWindow
{
    public IWindowRuleService WindowRuleService { get; }
    public MainWindow MainWindow { get; }

    public WindowRuleDebugViewModel ViewModel { get; } = new();

    public WindowRuleDebugWindow(IWindowRuleService windowRuleService, MainWindow mainWindow)
    {
        WindowRuleService = windowRuleService;
        MainWindow = mainWindow;
        InitializeComponent();
        DataContext = this;
    }

    private void WindowRuleDebugWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowRuleService.ForegroundWindowChanged += WindowRuleServiceOnForegroundWindowChanged;
        UpdateInfo();
    }

    private void WindowRuleServiceOnForegroundWindowChanged(object? sender, ForegroundWindowChangedEventArgs e)
    {
        UpdateInfo();
    }


    private void WindowRuleDebugWindow_OnUnloaded(object sender, RoutedEventArgs e)
    {
        WindowRuleService.ForegroundWindowChanged -= WindowRuleServiceOnForegroundWindowChanged;
    }

    private unsafe void UpdateInfo()
    {
        var hWnd = PlatformServices.WindowPlatformService.ForegroundWindowHandle;
        ViewModel.ForegroundWindowHandle = Convert.ToString(hWnd);
        try
        {
            ViewModel.ForegroundWindowClassName = PlatformServices.WindowPlatformService.GetWindowClassName(hWnd);
            ViewModel.ForegroundWindowTitle = PlatformServices.WindowPlatformService.GetWindowTitle(hWnd);
            UpdateWindowState(hWnd);
            var pid = PlatformServices.WindowPlatformService.GetWindowPid(hWnd);
            if (pid != 0)
            {
                var process = Process.GetProcessById(pid);
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

    private void UpdateWindowState(nint hWnd)
    {
        var screen = MainWindow.GetSelectedScreenSafe();
        if (screen == null)
        {
            return;
        }
        var fullscreen = PlatformServices.WindowPlatformService.IsForegroundWindowFullscreen(screen);
        var maximize = PlatformServices.WindowPlatformService.IsForegroundWindowMaximized(screen);
        var minimize = PlatformServices.WindowPlatformService.IsWindowMinimized(hWnd);

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