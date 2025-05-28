using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Theming;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
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

    public ITheme? CurrentTheme { get; set; } 

    public ILogger<ThemeService> Logger { get; }

    public event EventHandler<ThemeUpdatedEventArgs>? ThemeUpdated;

    public ThemeService(ILogger<ThemeService> logger)
    {
        Logger = logger;
    }

    public int CurrentRealThemeMode { get; set; } = 0;

    public void SetTheme(int themeMode, Color primary, Color secondary)
    {
        var theme = Application.Current!.LocateMaterialTheme<CustomMaterialTheme>();
        var lastPrimary = theme.PrimaryColor;
        var lastSecondary = theme.SecondaryColor;
        var useDarkTheme = false;
        switch (themeMode)
        {
            case 0:
                try
                {
                    var key = Registry.CurrentUser.OpenSubKey(
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                    if (key != null)
                    {
                        if ((int?)key.GetValue("AppsUseLightTheme") == 0)
                        {
                            useDarkTheme = true;
                        }
                        else
                        {
                            useDarkTheme = false;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogError(ex, "无法获取系统明暗主题，使用默认（亮色）主题。");
                    useDarkTheme = false;
                }
                break;

            case 1:
                useDarkTheme = false;
                break;
            case 2:
                useDarkTheme = true;
                break;
        }
        
        // TODO: 实现颜色调整
        // ((Theme)theme).ColorAdjustment = new ColorAdjustment()
        // {
        //     DesiredContrastRatio = 4.5F,
        //     Contrast = Contrast.Medium,
        //     Colors = ColorSelection.All
        // };
        
        if (lastPrimary == primary &&
            lastSecondary == secondary &&
            useDarkTheme == (theme.BaseTheme == BaseThemeMode.Dark ))
        {
            return;
        }

        var newTheme = Theme.Create(useDarkTheme ? Theme.Dark : Theme.Light, primary, secondary);
        theme.CurrentTheme = newTheme;
        CurrentTheme = newTheme;
        Logger.LogInformation("设置主题：{}", theme);
        CurrentRealThemeMode = useDarkTheme ? 1 : 0;
        ThemeUpdated?.Invoke(this, new ThemeUpdatedEventArgs
        {
            ThemeMode = themeMode,
            Primary = primary,
            Secondary = secondary,
            RealThemeMode = useDarkTheme ? 1 : 0
        });

        // var resource = new ResourceInclude(CurrentRealThemeMode == 0
        //     ? new Uri("avares://ClassIsland/Themes/LightTheme.axaml")
        //     : new Uri("avares://ClassIsland/Themes/DarkTheme.axaml"));
        // Application.Current!.Resources.MergedDictionaries[0] = resource;
    }
}