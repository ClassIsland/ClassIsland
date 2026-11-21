using Avalonia.Threading;
using ClassIsland.Controls.UI;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Views;

namespace ClassIsland.iOS.Services.SystemIntegration;

internal sealed class IosSystemNavigationService(MobileViewHost viewHost)
{
    private readonly SemaphoreSlim _navigationGate = new(1, 1);

    public async Task NavigateAsync(Uri uri)
    {
        if (!IosSystemNavigationRequest.TryParse(uri, out var request)) return;
        await _navigationGate.WaitAsync();
        try
        {
            var today = IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().Date;
            switch (request!.Action)
            {
                case "schedule":
                case "today":
                case "previous-day":
                case "next-day":
                {
                    var selected = IAppHost.GetService<HomeDateSelectionState>().SelectedDate ?? today;
                    var date = request.Action switch
                    {
                        "previous-day" => selected.AddDays(-1),
                        "next-day" => selected.AddDays(1),
                        _ => request.Date?.ToDateTime(TimeOnly.MinValue) ?? today
                    };
                    var view = await ShowMainViewAsync();
                    await view.ShowSystemScheduleAsync(date, request.LessonId);
                    break;
                }
                case "settings":
                    IAppHost.GetService<SettingsWindowNew>().Open();
                    break;
                case "profile":
                    IAppHost.GetService<ProfileSettingsWindow>().Open();
                    break;
                case "notifications":
                    await SetNotificationsAsync(request.NotificationsEnabled!.Value);
                    break;
                case "class-plan":
                    await SelectClassPlanAsync(request.ClassPlanId!.Value);
                    break;
                case "view-plan":
                {
                    var editor = IAppHost.GetService<ProfileSettingsWindow>();
                    if (!editor.ViewModel.SelectClassPlanByGuid(request.ClassPlanId!.Value))
                    {
                        await CommonTaskDialogs.ShowDialog("课表已不可用", "此课表可能已被删除，请在应用内选择其他课表。");
                        break;
                    }
                    editor.Open(new Uri("classisland://app/profile/classplans"));
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"无法处理系统课表操作：{exception}");
            await CommonTaskDialogs.ShowDialog("操作未完成", "请返回 ClassIsland 重试，或从应用内打开相应功能。");
        }
        finally
        {
            _navigationGate.Release();
        }
    }

    /// <summary>
    /// 执行菜单栏中针对主界面的命令。命令不经过 URL，外部链接无法触发。
    /// </summary>
    public async Task RunMainViewCommandAsync(Func<MainView, Task> command)
    {
        await _navigationGate.WaitAsync();
        try
        {
            await command(await ShowMainViewAsync());
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"无法执行菜单命令：{exception}");
            await CommonTaskDialogs.ShowDialog("操作未完成", "请返回 ClassIsland 重试，或从应用内打开相应功能。");
        }
        finally
        {
            _navigationGate.Release();
        }
    }

    private async Task<MainView> ShowMainViewAsync()
    {
        var view = viewHost.CurrentView as MainView ?? IAppHost.GetService<MainView>();
        if (!ReferenceEquals(viewHost.CurrentView, view)) view.Open();
        // 首次打开页面时先让宿主完成挂载，再切换页面、日期或打开抽屉。
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        return view;
    }

    private static async Task SetNotificationsAsync(bool enabled)
    {
        var management = IAppHost.GetService<IManagementService>();
        if (management.Policy.DisableSettingsEditing ||
            !await management.AuthorizeByLevel(management.CredentialConfig.EditSettingsAuthorizeLevel)) return;
        var service = IAppHost.GetService<SettingsService>();
        var previous = service.Settings.IsNotificationEnabled;
        service.Settings.IsNotificationEnabled = enabled;
        try
        {
            service.SaveSettings("通过系统操作设置课程提醒");
        }
        catch
        {
            service.Settings.IsNotificationEnabled = previous;
            throw;
        }
    }

    private static async Task SelectClassPlanAsync(Guid id)
    {
        var management = IAppHost.GetService<IManagementService>();
        if (!management.Policy.CanEditProfile ||
            !await management.AuthorizeByLevel(management.CredentialConfig.ChangeLessonsAuthorizeLevel)) return;
        var profile = IAppHost.GetService<IProfileService>();
        if (profile.Profile.ScheduleType != ScheduleType.Classic ||
            !profile.Profile.ClassPlans.TryGetValue(id, out var plan) || !plan.IsEnabled || plan.IsOverlay)
        {
            await CommonTaskDialogs.ShowDialog("无法选择此课表", "请在应用内检查课程模式和课表是否可用。");
            return;
        }
        if (profile.Profile.OverlayClassPlanId != null)
        {
            var editor = IAppHost.GetService<ProfileSettingsWindow>();
            editor.Open(new Uri("classisland://app/profile/adjustment"));
            await CommonTaskDialogs.ShowDialog("已有临时课表", "请在应用内确认或清除现有临时课表后重试。");
            return;
        }
        if (profile.CreateTempClassPlan(id) == null)
        {
            await CommonTaskDialogs.ShowDialog("无法加载临时课表", "请在应用内检查该课表后重试。");
            return;
        }
        profile.SaveProfile();
    }
}
