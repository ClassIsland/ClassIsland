using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class MainView : ViewBase
{
    public MainViewViewModel ViewModel { get; } = IAppHost.GetService<MainViewViewModel>();
    public ClassChangingWindow? ClassChangingWindow { get; set; }
    private Window? _scheduleWeekHost;
    

    public MainView()
    {
        InitializeComponent();
        MainViewTabs.SelectionChanged += MainViewTabs_OnSelectionChanged;
        MainNavigation.SelectionChanged += MainNavigation_OnSelectionChanged;
        MainNavigation.SelectedItem = HomeNavigationItem;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scheduleWeekHost = TopLevel.GetTopLevel(this) as Window;
        if (_scheduleWeekHost != null)
            _scheduleWeekHost.Activated += ScheduleWeekHost_OnActivated;
        RefreshScheduleWeekIfVisible();
        UpdateHomeCalendarActivation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ViewModel.DeactivateCalendar();
        if (_scheduleWeekHost != null)
            _scheduleWeekHost.Activated -= ScheduleWeekHost_OnActivated;
        _scheduleWeekHost = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void MainViewTabs_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = MainViewTabs.SelectedIndex switch
        {
            0 => HomeNavigationItem,
            1 => WeekNavigationItem,
            2 => MoreNavigationItem
        };
        if (MainNavigation.SelectedItem != item)
            MainNavigation.SelectedItem = item;
        RefreshScheduleWeekIfVisible();
        UpdateHomeCalendarActivation();
    }

    private void MainNavigation_OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem == HomeNavigationItem)
        {
            if (MainViewTabs.SelectedIndex != 0)
                MainViewTabs.SelectedIndex = 0;
        }
        else if (e.SelectedItem == WeekNavigationItem)
        {
            if (MainViewTabs.SelectedIndex != 1)
                MainViewTabs.SelectedIndex = 1;
        }
        else if (e.SelectedItem == MoreNavigationItem)
        {
            if (MainViewTabs.SelectedIndex != 2)
                MainViewTabs.SelectedIndex = 2;
        }
    }

    private void ScheduleWeekHost_OnActivated(object? sender, EventArgs e)
    {
        RefreshScheduleWeekIfVisible();
        ViewModel.RefreshCalendar();
    }

    private void UpdateHomeCalendarActivation()
    {
        if (MainViewTabs.SelectedIndex == 0 && VisualRoot != null)
            ViewModel.ActivateCalendar();
        else
            ViewModel.DeactivateCalendar();
    }

    private void RefreshScheduleWeekIfVisible()
    {
        if (MainViewTabs.SelectedIndex != 1) return;

        var today = DateOnly.FromDateTime(App.GetService<IExactTimeService>().GetCurrentLocalDateTime());
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        ScheduleWeekTest.WeekStart = weekStart;
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
        if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig.ExitApplicationAuthorizeLevel))
        {
            return;
        }

        if (PlatformHelper.IsAppleMobile)
        {
            ((App)AppBase.Current).PrepareForAppleMobileManualTermination();
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
        ViewModel.UriNavigationService.Navigate(new Uri("https://docs.classisland.tech/app/"));
    }

    private void MenuItemUpdates_OnClick(object sender, RoutedEventArgs e)
    {
        // App.GetService<SettingsWindowNew>().Open("update");
    }
    
    private void MenuItemClearAllNotifications_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.NotificationHostService.CancelAllNotifications();
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
        if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel))
        {
            return;
        }
        if (ViewModel.LessonsService.CurrentClassPlan == null) // 如果今天没有课程，则选择临时课表
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
            ClassPlan = ViewModel.LessonsService.CurrentClassPlan
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
