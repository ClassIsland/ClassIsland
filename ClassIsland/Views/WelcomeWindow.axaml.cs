using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Helpers;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using ClassIsland.Views.WelcomePages;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using WindowsShortcutFactory;

namespace ClassIsland.Views;

public partial class WelcomeWindow : MyWindow, INavigationPageFactory
{
   
    public static readonly ICommand WelcomeNavigateBackCommand = new RoutedCommand(nameof(WelcomeNavigateBackCommand));
    
    public static readonly ICommand WelcomeNavigateForwardCommand = new RoutedCommand(nameof(WelcomeNavigateForwardCommand));
    
    public static readonly ICommand FinishWelcomeWizardCommand = new RoutedCommand(nameof(FinishWelcomeWizardCommand));

    public WelcomeViewModel ViewModel { get; } = IAppHost.GetService<WelcomeViewModel>();

    public List<Type> Pages { get; } = [ 
        typeof(WelcomePage),
        typeof(LicensePage),
        typeof(GeneralPage),
        typeof(ColorThemePage),
        typeof(AppearancePage),
        typeof(SystemPage),
        typeof(FinishPage)
    ];

    private Dictionary<Type, object?> PageCache { get; } = new();
    
    public WelcomeWindow()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    // Create a page based on a Type, but you can create it however you want
    public Control? GetPage(Type srcType)
    {
        if (PageCache.TryGetValue(srcType, out var v) && v is Control control)
        {
            return control;
        }
        var page =  Activator.CreateInstance(srcType);
        if (page is IWelcomePage welcomePage)
        {
            welcomePage.ViewModel = ViewModel;
        }

        ViewModel.CurrentPage = srcType;
        PageCache[srcType] = page;
        return page as Control;
    }

    // Create a page based on an object, such as a view model
    public Control? GetPageFromObject(object target)
    {
        return null;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (AppBase.Current.PackagingType == "deb")
        {
            ViewModel.CreateStartupShortcut = false;
        }
        
        MainFrame.Navigate(typeof(WelcomePage));
    }

    private void CommandBindingNavigateForward_OnExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        var current = ViewModel.CurrentPage ?? Pages[0];
        var index = Math.Min(Pages.IndexOf(current) + 1, Pages.Count - 1);
        var type = Pages[index];
        ViewModel.CurrentPage = type;
        MainFrame.Navigate(type);
    }

    private void CommandBindingNavigateBack_OnExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        var current = ViewModel.CurrentPage ?? Pages[0];
        var index = Math.Max(Pages.IndexOf(current) - 1, 0);
        var type = Pages[index];
        ViewModel.CurrentPage = type;
        MainFrame.Navigate(type);
    }

    [SupportedOSPlatform("windows")]
    private async Task CreateShortcutsWindows()
    {
        var startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ClassIsland.lnk");
        var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "ClassIsland.lnk");
        var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ClassIsland.lnk");
        using var shortcut = new WindowsShortcut();
        shortcut.Path = AppBase.ExecutingEntrance;
        shortcut.WorkingDirectory = Path.GetDirectoryName(AppBase.ExecutingEntrance);
        if (ViewModel.CreateStartupShortcut)
            shortcut.Save(startupPath);
        if (ViewModel.CreateStartMenuShortcut)
            shortcut.Save(startMenuPath);
        if (ViewModel.CreateDesktopShortcut)
            shortcut.Save(desktopPath);
        if (ViewModel.RegisterUrlScheme)
            UriProtocolRegisterHelper.Register();
        if (ViewModel is { CreateClassSwapShortcut: true, RegisterUrlScheme: true })
            await ShortcutHelpers.CreateClassSwapShortcutAsync();
    }
    
    private async Task CreateShortcutsFreedesktop()
    {
        var startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config/autostart/cn.classisland.app.desktop");
        var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/share/applications/cn.classisland.app.desktop");
        var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ClassIsland.desktop");

        if (ViewModel.CreateStartupShortcut)
            await ShortcutHelpers.CreateFreedesktopShortcutAsync(startupPath);
        if (ViewModel.CreateStartMenuShortcut)
            await ShortcutHelpers.CreateFreedesktopShortcutAsync(startMenuPath);
        if (ViewModel.CreateDesktopShortcut)
            await ShortcutHelpers.CreateFreedesktopShortcutAsync(desktopPath);

        if (ViewModel.CreateStartupShortcut || ViewModel.CreateStartMenuShortcut || ViewModel.CreateDesktopShortcut)
        {
            await ShortcutHelpers.CopyFreeDesktopIconAsync();
        }
    }

    private async void CommandBindingFinishWizard_OnExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.CanClose = true;
        ViewModel.IsWizardCompleted = true;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                await CreateShortcutsWindows();
            }

            if (OperatingSystem.IsLinux())
            {
                await CreateShortcutsFreedesktop();
            }
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<WelcomeWindow>>().LogError(ex, "无法创建快捷方式。");
        }
        
        Close();
    }

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (ViewModel.CanClose || e.CloseReason is WindowCloseReason.OSShutdown or WindowCloseReason.ApplicationShutdown)
        {
            return;
        }

        e.Cancel = true;
        var r = await new ContentDialog()
        {
            Title = "退出 ClassIsland",
            Content = "您需要完成设置才能开始使用本应用。关闭此窗口将直接退出应用。",
            PrimaryButtonText = "退出",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync(this);
        if (r != ContentDialogResult.Primary)
        {
            return;
        }

        ViewModel.CanClose = true;
        Close();
    }
}