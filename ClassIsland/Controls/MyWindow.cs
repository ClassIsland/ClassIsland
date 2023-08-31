using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ClassIsland.Models;
using ClassIsland.Services;

namespace ClassIsland.Controls;

public class MyWindow : Window
{
    private ThemeService ThemeService { get; } = App.GetService<ThemeService>();

    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    public MyWindow()
    {
        ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateImmersiveDarkMode(ThemeService.CurrentRealThemeMode);
    }

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        UpdateImmersiveDarkMode(e.RealThemeMode);
    }

    private void UpdateImmersiveDarkMode(int mode)
    {
        var trueVal = 0x01;
        var falseVal = 0x00;
        if (mode == 0)
        {
            NativeWindowHelper.DwmSetWindowAttribute(new WindowInteropHelper(this).Handle,
                NativeWindowHelper.DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref falseVal,
                Marshal.SizeOf(typeof(int)));
        }
        else
        {
            NativeWindowHelper.DwmSetWindowAttribute(new WindowInteropHelper(this).Handle,
                NativeWindowHelper.DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref trueVal,
                Marshal.SizeOf(typeof(int)));
        }
    }
}