using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;

using ClassIsland.Models;
using ClassIsland.Services;

namespace ClassIsland.Controls;

public class MyWindow : Window
{
    private ThemeService? ThemeService { get; }

    public MyWindow()
    {
        try
        {
            ThemeService = App.GetService<ThemeService>();
            ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;
            App.GetService<HangService>().AssumeHang();
        }
        catch
        {
            // ignored
        }
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateImmersiveDarkMode(ThemeService?.CurrentRealThemeMode ?? 1);
        var layer = AdornerLayer.GetAdornerLayer(this);
        Debug.WriteLine(layer);
    }

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        UpdateImmersiveDarkMode(e.RealThemeMode);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        UpdateImmersiveDarkMode(ThemeService?.CurrentRealThemeMode ?? 1);
        Debug.WriteLine("rendered.");
    }

    private unsafe void UpdateImmersiveDarkMode(int mode)
    {
        var trueVal = 0x01;
        var falseVal = 0x00;
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        var build = Environment.OSVersion.Version.Build;
        if (build < 17763)
        {
            return;
        }
        //Debug.WriteLine(build);

        if (mode == 0)
        {
            DwmSetWindowAttribute(hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &falseVal,
                (uint)Marshal.SizeOf(typeof(int)));
        }
        else
        {
            DwmSetWindowAttribute(hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &trueVal,
                (uint)Marshal.SizeOf(typeof(int)));
        }

        // 在Windows10系统上强制刷新标题栏
        if (build < 22000)
        {
            uint WM_NCACTIVATE = 0x0086;
            SendMessage(hWnd, WM_NCACTIVATE, new WPARAM((nuint)(!IsActive ? 1 : 0)), 0);
            SendMessage(hWnd, WM_NCACTIVATE, new WPARAM((nuint)(IsActive ? 1 : 0)), 0);
        }
    }
}