using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
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
        var faTheme = FindFluentAvaloniaTheme(Application.Current!.Styles);
        if (faTheme == null)
        {
            return;
        }
        
        ThemeVariant? requestedThemeVariant = themeMode switch
        {
            1 => ThemeVariant.Light,
            2 => ThemeVariant.Dark,
            _ => null
        };

        // FluentAvalonia resolves and tracks the concrete system theme itself. Assigning
        // ThemeVariant.Default here makes Avalonia clear ActualThemeVariant and can re-enter
        // application resource resolution, eventually causing a stack overflow.
        faTheme.PreferSystemTheme = requestedThemeVariant == null;
        if (requestedThemeVariant != null)
        {
            AppBase.Current.RequestedThemeVariant = requestedThemeVariant;
        }

        faTheme.CustomAccentColor = primary;
        faTheme.PreferUserAccentColor = primary == null;
        
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
    }

    private static FluentAvaloniaTheme? FindFluentAvaloniaTheme(IStyle root)
    {
        // 全局样式外包了一层 ResourceLookupCachingStyles 缓存容器，FluentAvaloniaTheme 嵌套在容器内，
        // 顶层 OfType 无法找到，需按 IStyle.Children 递归查找（保持先序遍历以维持原顺序语义）。
        if (root is FluentAvaloniaTheme theme)
        {
            return theme;
        }

        foreach (var child in root.Children)
        {
            var found = FindFluentAvaloniaTheme(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
