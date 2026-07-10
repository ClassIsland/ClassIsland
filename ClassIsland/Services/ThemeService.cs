using System;
using System.Linq;
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
        var faTheme = Application.Current!.Styles
            .OfType<FluentAvaloniaTheme>()
            .FirstOrDefault();
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
}
