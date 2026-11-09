using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Theming;
using ClassIsland.Models;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class ThemeService : IHostedService, IThemeService
{
    private SettingsService? _settingsService;
    private Settings? _settings;
    private INotifyCollectionChanged? _wallpaperPalette;
    private IPlatformSettings? _platformSettings;
    private TopLevel? _mainWindowThemeHost;
    private ResourceDictionary? _mainWindowAccentResources;
    private Color? _mainWindowAccent;

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

    internal void Initialize(SettingsService settingsService)
    {
        _settingsService = settingsService;
        settingsService.PropertyChanged += SettingsServiceOnPropertyChanged;
        AppBase.Current.AppStopping += AppOnStopping;
        ObserveSettings();

        _platformSettings = AppBase.Current.PlatformSettings;
        if (_platformSettings != null)
        {
            _platformSettings.ColorValuesChanged += PlatformSettingsOnColorValuesChanged;
        }
    }

    internal void SetMainWindowThemeHost(TopLevel? host)
    {
        if (ReferenceEquals(host, _mainWindowThemeHost))
        {
            return;
        }

        _mainWindowThemeHost?.ClearValue(TopLevel.RequestedThemeVariantProperty);
        SetMainWindowAccent(null);
        _mainWindowThemeHost = host;
        UpdateMainWindowTheme();
    }

    private void SettingsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.Settings))
        {
            RunOnUiThread(ObserveSettings);
        }
    }

    private void ObserveSettings()
    {
        if (_settingsService == null)
        {
            return;
        }

        if (_settings != null)
        {
            _settings.PropertyChanged -= SettingsOnPropertyChanged;
        }
        _settings = _settingsService.Settings;
        _settings.PropertyChanged += SettingsOnPropertyChanged;
        ObserveWallpaperPalette(_settings.WallpaperColorPlatte);
        UpdateApplicationTheme();
        UpdateMainWindowTheme();
    }

    private void ObserveWallpaperPalette(INotifyCollectionChanged? palette)
    {
        if (_wallpaperPalette != null)
        {
            _wallpaperPalette.CollectionChanged -= WallpaperPaletteOnCollectionChanged;
        }
        _wallpaperPalette = palette;
        if (_wallpaperPalette != null)
        {
            _wallpaperPalette.CollectionChanged += WallpaperPaletteOnCollectionChanged;
        }
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.WallpaperColorPlatte):
                RunOnUiThread(() =>
                {
                    ObserveWallpaperPalette(_settings?.WallpaperColorPlatte);
                    UpdateApplicationTheme();
                });
                break;
            case nameof(Settings.Theme):
            case nameof(Settings.ColorSource):
            case nameof(Settings.PrimaryColor):
            case nameof(Settings.SelectedPlatteIndex):
                RunOnUiThread(UpdateApplicationTheme);
                break;
            case nameof(Settings.IsMainWindowThemeOverrideEnabled):
            case nameof(Settings.MainWindowTheme):
            case nameof(Settings.IsMainWindowColorSourceOverrideEnabled):
            case nameof(Settings.MainWindowColorSource):
            case nameof(Settings.MainWindowPrimaryColor):
                RunOnUiThread(UpdateMainWindowTheme);
                break;
        }
    }

    private void WallpaperPaletteOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_settings?.ColorSource is 1 or 3)
        {
            RunOnUiThread(UpdateApplicationTheme);
        }
    }

    private void PlatformSettingsOnColorValuesChanged(object? sender, PlatformColorValues e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_settings?.ColorSource == 2)
            {
                UpdateApplicationAccentBrush();
            }
            UpdateMainWindowTheme();
        });
    }

    private void AppOnStopping(object? sender, EventArgs e)
    {
        AppBase.Current.AppStopping -= AppOnStopping;
        if (_platformSettings != null)
        {
            _platformSettings.ColorValuesChanged -= PlatformSettingsOnColorValuesChanged;
        }
        if (_settingsService != null)
        {
            _settingsService.PropertyChanged -= SettingsServiceOnPropertyChanged;
            _settingsService = null;
        }
        if (_settings != null)
        {
            _settings.PropertyChanged -= SettingsOnPropertyChanged;
            _settings = null;
        }
        ObserveWallpaperPalette(null);
        SetMainWindowThemeHost(null);
    }

    private static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    private void UpdateApplicationTheme()
    {
        if (_settings is not { } settings)
        {
            return;
        }

        Color? primary = settings.ColorSource switch
        {
            0 => settings.PrimaryColor,
            1 or 3 when settings.SelectedPlatteIndex >= 0 => settings.SelectedPlatte,
            2 => null,
            _ => Colors.DodgerBlue
        };
        SetTheme(settings.Theme, primary);
    }

    private void UpdateMainWindowTheme()
    {
        if (_mainWindowThemeHost is not { } host || _settings is not { } settings)
        {
            return;
        }

        var systemColors = _platformSettings?.GetColorValues();
        if (settings.IsMainWindowThemeOverrideEnabled)
        {
            host.RequestedThemeVariant = GetExplicitThemeVariant(settings.MainWindowTheme)
                ?? (systemColors?.ContrastPreference == ColorContrastPreference.High
                    ? FluentAvaloniaTheme.HighContrastTheme
                    : systemColors?.ThemeVariant == PlatformThemeVariant.Dark
                        ? ThemeVariant.Dark
                        : ThemeVariant.Light);
        }
        else
        {
            host.ClearValue(TopLevel.RequestedThemeVariantProperty);
        }

        Color? accent = settings.IsMainWindowColorSourceOverrideEnabled
            ? settings.MainWindowColorSource == 0
                ? settings.MainWindowPrimaryColor
                : systemColors?.AccentColor1 ?? Colors.SlateBlue
            : null;
        SetMainWindowAccent(accent);
    }

    private void SetMainWindowAccent(Color? accent)
    {
        if (_mainWindowThemeHost is not { } host || accent == _mainWindowAccent)
        {
            return;
        }

        if (_mainWindowAccentResources != null)
        {
            host.Resources.MergedDictionaries.Remove(_mainWindowAccentResources);
        }
        _mainWindowAccent = accent;
        _mainWindowAccentResources = accent is { } color ? CreateAccentResources(color) : null;
        if (_mainWindowAccentResources != null)
        {
            host.Resources.MergedDictionaries.Add(_mainWindowAccentResources);
        }
    }

    private static ResourceDictionary CreateAccentResources(Color accent)
    {
        // FluentAvalonia's shared brushes resolve colors at the application level.
        // Supply local brushes as well so dynamic resource lookups keep the override scoped.
        Color2 color = accent;
        var light1 = (Color)color.LightenPercent(0.15f);
        var light2 = (Color)color.LightenPercent(0.30f);
        var light3 = (Color)color.LightenPercent(0.45f);
        var dark1 = (Color)color.LightenPercent(-0.15f);
        var dark2 = (Color)color.LightenPercent(-0.30f);
        var dark3 = (Color)color.LightenPercent(-0.45f);

        return new ResourceDictionary
        {
            ["SystemAccentColor"] = accent,
            ["SystemAccentColorLight1"] = light1,
            ["SystemAccentColorLight2"] = light2,
            ["SystemAccentColorLight3"] = light3,
            ["SystemAccentColorDark1"] = dark1,
            ["SystemAccentColorDark2"] = dark2,
            ["SystemAccentColorDark3"] = dark3,
            ["CustomizedAccentBarBackground1Brush"] = new SolidColorBrush(accent, 0.10),
            ThemeDictionaries =
            {
                [ThemeVariant.Light] = CreateAccentBrushes(accent, dark1, dark2, dark3, accent),
                [ThemeVariant.Dark] = CreateAccentBrushes(accent, light2, light3, light3, light2)
            }
        };
    }

    private static ResourceDictionary CreateAccentBrushes(Color accent, Color fill, Color text,
        Color secondaryText, Color attention)
    {
        return new ResourceDictionary
        {
            ["AccentFillColorSelectedTextBackgroundBrush"] = new SolidColorBrush(accent),
            ["AccentFillColorDefaultBrush"] = new SolidColorBrush(fill),
            ["AccentFillColorSecondaryBrush"] = new SolidColorBrush(fill, 0.9),
            ["AccentFillColorTertiaryBrush"] = new SolidColorBrush(fill, 0.8),
            ["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(text),
            ["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(secondaryText),
            ["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(fill),
            ["SystemFillColorAttentionBrush"] = new SolidColorBrush(attention)
        };
    }

    public void SetTheme(int themeMode, Color? primary)
    {
        var faTheme = FindFluentAvaloniaTheme(Application.Current!.Styles);
        if (faTheme == null)
        {
            return;
        }
        
        var requestedThemeVariant = GetExplicitThemeVariant(themeMode);

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

        UpdateApplicationAccentBrush();
    }

    private static ThemeVariant? GetExplicitThemeVariant(int themeMode) => themeMode switch
    {
        1 => ThemeVariant.Light,
        2 => ThemeVariant.Dark,
        _ => null
    };

    private static void UpdateApplicationAccentBrush()
    {
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
