using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ClassIsland.Core;
using ClassIsland.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Controls.ScheduleWeekEdit;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using ClassIsland.Views.MainViewParts;
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
        ViewModel.ProfileService.Profile.PropertyChanged -= Profile_OnPropertyChanged;
        ViewModel.ProfileService.Profile.PropertyChanged += Profile_OnPropertyChanged;
        _scheduleWeekHost = TopLevel.GetTopLevel(this) as Window;
        if (_scheduleWeekHost != null)
            _scheduleWeekHost.Activated += ScheduleWeekHost_OnActivated;
        RefreshScheduleWeekIfVisible();
        UpdateHomeCalendarActivation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ViewModel.ProfileService.Profile.PropertyChanged -= Profile_OnPropertyChanged;
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

        ViewModel.RefreshScheduleWeek();
    }

    private void ScheduleWeekPrevious_OnClick(object? sender, RoutedEventArgs e) => ViewModel.MoveScheduleWeek(-1);
    private void ScheduleWeekNext_OnClick(object? sender, RoutedEventArgs e) => ViewModel.MoveScheduleWeek(1);
    private void ScheduleWeekToday_OnClick(object? sender, RoutedEventArgs e) => ViewModel.GoToCurrentScheduleWeek();

    private void Profile_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == nameof(Profile.ScheduleType))
        {
            ViewModel.RefreshScheduleModeState();
        }
    }

    private void EditScheduleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanEditMobileSchedule)
        {
            return;
        }
        ViewModel.IsScheduleEditing = true;
    }

    private void FinishEditingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsScheduleEditing = false;
    }

    private async void AddScheduleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!await AuthorizeCourseEditAsync())
        {
            return;
        }

        var today = App.GetService<IExactTimeService>().GetCurrentLocalDateTime();
        var date = MainViewTabs.SelectedIndex == 1
            ? ScheduleWeekTest.SelectedDate ?? ScheduleWeekTest.WeekStart.AddDays(((int)today.DayOfWeek + 6) % 7)
            : DateOnly.FromDateTime(ViewModel.SelectedDate);
        var time = MainViewTabs.SelectedIndex == 1
            ? ScheduleWeekTest.SelectedTime
            : null;
        try
        {
            var created = ViewModel.CreateScheduleItem(date, time);
            await ShowCourseEditorAsync(created.Id, date);
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法新建课程", exception);
        }
    }

    private async Task<bool> AuthorizeCourseEditAsync()
    {
        if (!ViewModel.CanEditMobileSchedule)
        {
            ToastsHelper.ShowWarningToast(this, "当前无法编辑课程，请检查档案编辑权限。");
            return false;
        }
        return await ViewModel.ManagementService.AuthorizeByLevel(
            ViewModel.ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel);
    }

    private async Task<bool> AuthorizeProfileEditAsync()
    {
        if (!ViewModel.CanEditProfileFeatures)
        {
            ToastsHelper.ShowWarningToast(this, "当前无法编辑档案，请检查档案编辑权限。");
            return false;
        }
        return await ViewModel.ManagementService.AuthorizeByLevel(
            ViewModel.ManagementService.CredentialConfig.EditProfileAuthorizeLevel);
    }

    private async Task ShowCourseInfoAsync(Guid scheduleItemId, DateOnly date)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        await AssociatedViewHost.ShowDrawer(new CourseInfoView(scheduleItemId, date));
    }

    private async Task ShowClassicCourseInfoAsync(ClassInfo classInfo, DateOnly date)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        await AssociatedViewHost.ShowDrawer(new CourseInfoView(classInfo, date));
    }

    private async Task ShowCourseEditorAsync(Guid scheduleItemId, DateOnly date)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        await AssociatedViewHost.ShowDrawer(new CourseEditView(scheduleItemId, date));
    }

    private async void MainMoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control anchor || AssociatedViewHost == null)
        {
            return;
        }

        var menu = new MenuFlyout();
        var profile = new MenuItem
        {
            Header = $"当前档案：{System.IO.Path.GetFileName(ViewModel.ProfileService.CurrentProfilePath)}",
            Icon = new FluentIcon(Core.Icons.FluentIcons.PersonRegular)
        };
        profile.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowManagementDrawerAsync(new ProfileManagementView());
            }
        };
        var mode = new MenuItem
        {
            Header = $"课程模式：{(ViewModel.IsScheduleMode ? "日程" : "经典")}",
            Icon = new FluentIcon(Core.Icons.FluentIcons.OptionsRegular)
        };
        mode.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowManagementDrawerAsync(new ScheduleModeView());
            }
        };
        var transfer = new MenuItem
        {
            Header = "导入/导出课表",
            Icon = new FluentIcon(Core.Icons.FluentIcons.ArrowImportRegular)
        };
        transfer.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowManagementDrawerAsync(new ImportExportView());
            }
        };
        var subjects = new MenuItem
        {
            Header = "管理科目…",
            Icon = new FluentIcon(Core.Icons.FluentIcons.BookOpenRegular)
        };
        subjects.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowSubjectManagementAsync();
            }
        };
        menu.Items.Add(profile);
        menu.Items.Add(mode);
        menu.Items.Add(transfer);
        menu.Items.Add(subjects);
        menu.ShowAt(anchor);
    }

    private async Task ShowManagementDrawerAsync(ViewBase view)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }
        await AssociatedViewHost.ShowDrawer(view);
    }

    private async Task ShowSubjectManagementAsync()
    {
        if (AssociatedViewHost == null)
        {
            return;
        }
        var host = AssociatedViewHost;
        var view = new SubjectManagementView();
        if (host.ActivateView(view))
        {
            await host.ShowView(view);
        }
    }

    private async void HomeLessonsList_OnTapped(object? sender, TappedEventArgs e)
    {
        var point = e.GetPosition(HomeLessonsList);
        var item = HomeLessonsList.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(container =>
            {
                var topLeft = container.TranslatePoint(default, HomeLessonsList);
                return topLeft is { } position &&
                       new Rect(position, container.Bounds.Size).Contains(point);
            });
        if (item?.DataContext is not TimeLayoutItem timeItem)
        {
            return;
        }

        var date = DateOnly.FromDateTime(ViewModel.SelectedDate);
        if (ViewModel.IsScheduleMode)
        {
            var id = ViewModel.FindScheduleItemIdByLesson(ViewModel.SelectedDate, timeItem);
            if (id is not { } scheduleItemId)
            {
                return;
            }
            if (ViewModel.IsScheduleEditing)
            {
                if (await AuthorizeCourseEditAsync())
                {
                    await ShowCourseEditorAsync(scheduleItemId, date);
                }
            }
            else
            {
                await ShowCourseInfoAsync(scheduleItemId, date);
            }
            return;
        }

        var classInfo = ViewModel.FindClassInfoByLesson(ViewModel.SelectedDate, timeItem);
        if (classInfo != null)
        {
            await ShowClassicCourseInfoAsync(classInfo, date);
        }
    }

    private async void ScheduleWeekEditor_OnCourseTapped(object? sender, ScheduleWeekEditEventArgs e)
    {
        if (e.ScheduleItemId is not { } id)
        {
            return;
        }

        if (ViewModel.IsScheduleEditing)
        {
            if (await AuthorizeCourseEditAsync())
            {
                await ShowCourseEditorAsync(id, e.Date);
            }
            return;
        }

        await ShowCourseInfoAsync(id, e.Date);
    }

    private async void ScheduleWeekEditor_OnCreateRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        if (!await AuthorizeCourseEditAsync())
        {
            return;
        }
        try
        {
            var created = ViewModel.CreateScheduleItem(e.Date, e.StartTime);
            await ShowCourseEditorAsync(created.Id, e.Date);
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法新建课程", exception);
        }
    }

    private async void ScheduleWeekEditor_OnEditRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        if (!await AuthorizeCourseEditAsync())
        {
            return;
        }
        try
        {
            if (!ViewModel.ApplyScheduleWeekEdit(e))
            {
                ToastsHelper.ShowWarningToast(this, "无法移动到该日期或时间，请检查课程的时间和启用范围。");
            }
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法保存课程移动", exception);
        }
    }

    private async void ScheduleWeekEditor_OnDeleteRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        if (e.ScheduleItemId is not { } id || !await AuthorizeCourseEditAsync() ||
            !ViewModel.ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item))
        {
            return;
        }
        var result = await new FAContentDialog
        {
            Title = "删除课程",
            Content = "删除后，这门课程的所有重复安排都会被移除。",
            PrimaryButtonText = "删除",
            SecondaryButtonText = "取消",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsyncAuto();
        if (result != FAContentDialogResult.Primary)
        {
            return;
        }
        ViewModel.ProfileService.Profile.ScheduleItems.Remove(id);
        try
        {
            ViewModel.ProfileService.SaveProfile();
        }
        catch (Exception exception)
        {
            ViewModel.ProfileService.Profile.ScheduleItems.Add(id, item);
            ToastsHelper.ShowErrorToast(this, "无法删除课程", exception);
        }
    }

    private async void ScheduleWeekEditor_OnDuplicateRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        if (e.ScheduleItemId is not { } id || !await AuthorizeCourseEditAsync() ||
            !ViewModel.ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item))
        {
            return;
        }
        var copy = ConfigureFileHelper.CopyObject(item);
        var copyId = Guid.NewGuid();
        ViewModel.ProfileService.Profile.ScheduleItems.Add(copyId, copy);
        try
        {
            ViewModel.ProfileService.SaveProfile();
        }
        catch (Exception exception)
        {
            ViewModel.ProfileService.Profile.ScheduleItems.Remove(copyId);
            ToastsHelper.ShowErrorToast(this, "无法复制课程", exception);
        }
    }

    private async void ScheduleWeekEditor_OnForkAndReplaceRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        if (e.ScheduleItemId is not { } id || !await AuthorizeCourseEditAsync())
        {
            return;
        }
        var newId = ViewModel.ProfileService.ForkAndReplaceScheduleItem(
            id, e.Date.ToDateTime(TimeOnly.MinValue));
        if (newId == null)
        {
            ToastsHelper.ShowWarningToast(this, "无法创建临时覆盖课程，请重新选择课程后重试。");
            return;
        }
        try
        {
            ViewModel.ProfileService.SaveProfile();
            ToastsHelper.ShowToast(this, "已创建临时覆盖课程。");
        }
        catch (Exception exception)
        {
            ViewModel.ProfileService.Profile.ScheduleItems.Remove(newId.Value);
            ToastsHelper.ShowErrorToast(this, "无法创建课程覆盖", exception);
        }
    }

    private void ScheduleWeekEditor_OnDayHeaderClicked(object? sender, ScheduleDayHeaderEventArgs e)
    {
        ScheduleAdjustmentMenu.Show(this, e.Anchor, e.Date, AuthorizeCourseEditAsync);
        e.Handled = true;
    }
    
    
    private void ButtonSettings_OnClick(object sender, RoutedEventArgs e)
    {
        IAppHost.GetService<ProfileSettingsWindow>().Open();
    }

    private void ButtonAdjustSchedule_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanAdjustSchedule || sender is not Control anchor) return;
        ScheduleAdjustmentMenu.Show(this, anchor, DateOnly.FromDateTime(ViewModel.SelectedDate),
            () => ViewModel.ManagementService.AuthorizeByLevel(
                ViewModel.ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel));
        e.Handled = true;
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
