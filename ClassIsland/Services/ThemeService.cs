using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Theming;
using FluentAvalonia.Styling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClassIsland.Services;

public class ThemeService : IHostedService, IThemeService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
    

    public ILogger<ThemeService> Logger { get; }

    public event EventHandler<ThemeUpdatedEventArgs>? ThemeUpdated;

    public ThemeService(ILogger<ThemeService> logger)
    {
        Logger = logger;
    }

    public int CurrentRealThemeMode { get; set; } = 0;

    public void SetTheme(int themeMode, Color? primary)
    {
        var faTheme = Application.Current!.Styles
            .OfType<FluentAvaloniaTheme>()
            .FirstOrDefault();
        if (faTheme == null)
        {
            return;
        }
        
        AppBase.Current.RequestedThemeVariant = themeMode switch
        {
            0 => ThemeVariant.Default,
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        faTheme.CustomAccentColor = primary;
        faTheme.PreferUserAccentColor = primary == null;
        faTheme.PreferSystemTheme = themeMode == 0;
        
        // 计算应用画刷
        var brush = AppBase.Current.TryFindResource("AccentFillColorSelectedTextBackgroundBrush",
            ThemeVariant.Default, out var v)
            ? v as SolidColorBrush
            : null;
        var newBrush = brush == null ? null : new SolidColorBrush(brush.Color, 0.10);
        if (newBrush != null)
        {
            AppBase.Current.Resources["CustomizedAccentBarBackground1Brush"] = newBrush;
        }

        // 设置主题后通知订阅者刷新；跟随系统时由平台外观计算事件中的实际模式，
        // 避免依赖尚未完成传播的应用主题属性。
        var realThemeMode = themeMode switch
        {
            1 => 0,
            2 => 1,
            _ => AppBase.Current.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark
                ? 1
                : 0
        };
        CurrentRealThemeMode = realThemeMode;
        ThemeUpdated?.Invoke(this, new ThemeUpdatedEventArgs
        {
            ThemeMode = themeMode,
            Primary = primary ?? AppBase.Current.PlatformSettings?.GetColorValues().AccentColor1 ?? Colors.DodgerBlue,
            RealThemeMode = realThemeMode
        });
    }
}