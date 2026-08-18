using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Services;
using ClassIsland.Services.Management;
using ClassIsland.Shared;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class MainView : ViewBase
{
    public IManagementService ManagementService { get; }
    public IUriNavigationService UriNavigationService { get; }
    public INotificationHostService NotificationHostService { get; }
    public ILessonsService LessonsService { get; }
    public ClassChangingWindow? ClassChangingWindow { get; set; }
    

    public MainView(IManagementService managementService,
        IUriNavigationService uriNavigationService,
        INotificationHostService notificationHostService,
        ILessonsService lessonsService)
    {
        ManagementService = managementService;
        UriNavigationService = uriNavigationService;
        NotificationHostService = notificationHostService;
        LessonsService = lessonsService;
        InitializeComponent();
    }
    
    
    private void ButtonSettings_OnClick(object sender, RoutedEventArgs e)
    {
        IAppHost.GetService<ProfileSettingsWindow>().Open();
    }

    private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<SettingsWindowNew>().Open();
    }


    private async void MenuItemExitApp_OnClick(object sender, RoutedEventArgs e)
    {
        if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.ExitApplicationAuthorizeLevel))
        {
            return;
        }
        
        Close();
        AppBase.Current.Stop();
    }
    private void MenuItemRestartApp_OnClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart();
    }
    
    private void MenuItemTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var window = App.GetService<ProfileSettingsWindow>();
        window.OpenDrawer("TemporaryClassPlan");
        window.Open();
    }
    
    private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<SettingsWindowNew>().Open("about");
    }

    private void MenuItemHelps_OnClick(object sender, RoutedEventArgs e)
    {
        UriNavigationService.Navigate(new Uri("https://docs.classisland.tech/app/"));
    }

    private void MenuItemUpdates_OnClick(object sender, RoutedEventArgs e)
    {
        // App.GetService<SettingsWindowNew>().Open("update");
    }
    
    private void MenuItemClearAllNotifications_OnClick(object sender, RoutedEventArgs e)
    {
        NotificationHostService.CancelAllNotifications();
    }

    private void MenuItemNotificationSettings_OnClick(object sender, RoutedEventArgs e)
    {
        // App.GetService<SettingsWindowNew>().Open("notification");
    }

    private void MenuItemClassSwap_OnClick(object sender, RoutedEventArgs e)
    {
        OpenClassSwapWindow();
    }
    
    private async void OpenClassSwapWindow()
    {
        if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel))
        {
            return;
        }
        if (LessonsService.CurrentClassPlan == null) // 如果今天没有课程，则选择临时课表
        {
            var window = App.GetService<ProfileSettingsWindow>();
            window.OpenDrawer("TemporaryClassPlan");
            window.Open();
            return;
        }

        if (ClassChangingWindow != null)
        {
            return;
        }
        
        // ViewModel.IsBusy = true;
        ClassChangingWindow = new ClassChangingWindow()
        {
            ClassPlan = LessonsService.CurrentClassPlan
        };
        await ClassChangingWindow.ShowModal(this);
        ClassChangingWindow.DataContext = null;
        ClassChangingWindow = null;
        // ViewModel.IsBusy = false;
    }


    private void NativeMenuItemDebugDevTools_OnClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new KeyEventArgs()
        {
            Key = Key.F12,
            RoutedEvent = KeyDownEvent
        });
    }

    private void NativeMenuItemDebugCrashTest_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new CrashWindow();
        window.Show();
    }

    private void NativeMenuItemDebugDevPortal_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.GetService<DevPortalWindow>().Show();
    }
    
    private void NativeMenuItemDebugOpenWelcomeWindow_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.GetService<WelcomeWindow>().Show();
    }
    
    private void NativeMenuItemOpenTutorialEditor_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.GetService<TutorialEditorWindow>().Show();
    }

    private void NativeMenuItemDebugOpenScreenshotWindow_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.GetService<ScreenshotHelperWindow>().Show();
    }
    
    private void NativeMenuItemTutorials_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.GetService<TutorialCenterWindow>().Open();
    }

    private void FASettingsExpanderDevPortal_OnClick(object? sender, RoutedEventArgs e)
    {
        new DevPortalWindow().Show();
    }
}
