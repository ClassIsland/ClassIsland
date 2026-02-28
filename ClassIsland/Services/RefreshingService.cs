using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Views;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Services;

public class RefreshingService(SettingsService settingsService) : IRefreshingService
{
    public const string DefaultOnboardingToastTitle = "欢迎使用 ClassIsland (/≧▽≦)/";

    public const string DefaultOnboardingToastBody =
        "屏幕上的课表是由本大屏课表软件 ClassIsland 显示的。ClassIsland 能一目了然地显示各种信息，并支持高度自定义。要了解如何使用 ClassIsland 吗？";

    public const string DefaultRefreshingToastTitle = "翻新 ClassIsland";

    public const string DefaultRefreshingToastBody = "ClassIsland 似乎已经有一个长假没有启动了，部分设置可能已经过期。要跟随欢迎向导重新设置如学期开始时间等设置吗？";
    
    public SettingsService SettingsService { get; } = settingsService;
    
    public async Task Initialize()
    {
        
    }

    public async Task ShowOnboardingDialog(bool isTest=false)
    {
        var r = await new TaskDialog()
        {
            Header = SettingsService.Settings.OnboardingToastTitle,
            Content = SettingsService.Settings.OnboardingToastBody,
            Buttons =
            [
                new TaskDialogButton("退出并不再显示", 0),
                new TaskDialogButton("以后再说", 1),
                new TaskDialogButton("好",2)
                {
                    IsDefault = true
                },
            ],
            XamlRoot = AppBase.Current.GetRootWindow(),
            Title = "欢迎使用 ClassIsland"
        }.ShowAsync();
        if (Equals(r, 0))
        {
            PlatformServices.DesktopService.IsAutoStartEnabled = false;
            SettingsService.Settings.LeftRefreshingToastCounts = 0;
            AppBase.Current.Stop();
            return;
        }
        if (!Equals(r, 2))
        {
            return;
        }

        SettingsService.Settings.LeftRefreshingToastCounts = 0;
        await BeginRefresh(true);
    }

    public async Task BeginRefresh(bool isOnboarding=false)
    {
        var welcomeWin = IAppHost.GetService<WelcomeWindow>();
        welcomeWin.ViewModel.RefreshingScopes =
            ConfigureFileHelper.CopyObject(SettingsService.Settings.RefreshingScopes);
        welcomeWin.SetWelcomeExperience(true, isOnboarding, false);
        await welcomeWin.ShowDialog(AppBase.Current.GetRootWindow());
    }
}