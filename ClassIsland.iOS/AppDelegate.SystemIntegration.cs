using Avalonia.Threading;
using ClassIsland.Shared;
using ClassIsland.Views;
using ClassIsland.iOS.Services.SystemIntegration;
using ClassIsland.Platforms.Abstraction.Services;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace ClassIsland.iOS;

public sealed partial class AppDelegate
{
    private IosSystemNavigationService? _systemNavigationService;
    private IosSystemIntegrationCoordinator? _systemIntegrationCoordinator;
    private bool _systemNavigationRegistered;
    private bool _isMainMenuInstalledBySystem;

    private void TryRegisterSystemNavigation()
    {
        try
        {
            IosSystemIntegrationBridge.RegisterNavigation();
            _systemNavigationRegistered = true;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"无法注册系统课表入口：{exception}");
        }

        try
        {
            _isMainMenuInstalledBySystem = IosSystemIntegrationBridge.InstallMainMenu();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"无法注册菜单栏课表菜单：{exception}");
        }
    }

    private void ConsumePendingSystemNavigation()
    {
        if (!_systemNavigationRegistered) return;
        try
        {
            IosSystemIntegrationBridge.ConsumePendingNavigation();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"无法读取待处理的系统课表操作：{exception}");
        }
    }

    internal void RequestNavigation(Uri uri)
    {
        if (_isHudProcess || !AppNavigationUriParser.TryParseClassIslandUri(uri.AbsoluteUri, out _)) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (_isAppNavigationReady)
            {
                QueueNavigation(uri);
            }
            else
            {
                if (_pendingNavigationUris.Count >= 32) _pendingNavigationUris.Dequeue();
                _pendingNavigationUris.Enqueue(uri);
            }
        });
    }

    internal bool HandleSystemActivity(NSUserActivity activity)
    {
        if (_isHudProcess || activity.ActivityType != "com.apple.corespotlightitem") return false;
        using var identifierKey = new NSString("kCSSearchableItemActivityIdentifier");
        if (!AppNavigationUriParser.TryParseClassIslandUri(
                activity.UserInfo?[identifierKey]?.ToString(), out var uri)) return false;
        RequestNavigation(uri!);
        return true;
    }

    // Avalonia 的默认场景只转交网页 Handoff，需要显式接收 Spotlight 的活动。
    [Export("application:configurationForConnectingSceneSession:options:")]
    public new UISceneConfiguration GetConfiguration(
        UIApplication application, UISceneSession session, UISceneConnectionOptions options)
    {
        var configuration = base.GetConfiguration(application, session, options);
        if (!_isHudProcess) configuration.DelegateType = typeof(IosSystemSceneDelegate);
        return configuration;
    }

    [Export("application:continueUserActivity:restorationHandler:")]
    public new bool ContinueUserActivity(
        UIApplication application, NSUserActivity activity, UIApplicationRestorationHandler completionHandler) =>
        HandleSystemActivity(activity) || base.ContinueUserActivity(application, activity, completionHandler);

    public override void BuildMenu(IUIMenuBuilder builder)
    {
        base.BuildMenu(builder);
        // iPadOS 26 起菜单由 UIMainMenuSystem 构建，再次插入同一标识符的菜单会冲突。
        if (_isHudProcess || _isMainMenuInstalledBySystem || builder.System != UIMenuSystem.MainSystem) return;
        var menu = UIMenu.Create("课表", null, new NSString("cn.classisland.schedule"), (UIMenuOptions)0,
        [
            CreateScheduleCommand("今日课表", "t", UIKeyModifierFlags.Command, "ciToday:"),
            CreateScheduleCommand("前一天", "[", UIKeyModifierFlags.Command, "ciPreviousDay:"),
            CreateScheduleCommand("后一天", "]", UIKeyModifierFlags.Command, "ciNextDay:"),
            CreateScheduleCommand("换课…", "k", UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, "ciClassSwap:"),
            CreateScheduleCommand("加载临时课表…", "t", UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift,
                "ciTemporaryClassPlan:"),
            CreateScheduleCommand("编辑档案…", "e", UIKeyModifierFlags.Command | UIKeyModifierFlags.Shift, "ciProfile:"),
            CreateScheduleCommand("设置…", ",", UIKeyModifierFlags.Command, "ciSettings:")
        ]);
        builder.InsertChildMenuAtEnd(menu, UIMenuIdentifier.Root.GetConstant()!);
        builder.InsertChildMenuAtStart(UIMenu.Create("", null, new NSString("cn.classisland.view"),
            UIMenuOptions.DisplayInline,
            [
                CreateScheduleCommand("今天", "1", UIKeyModifierFlags.Command, "ciShowTodayPage:"),
                CreateScheduleCommand("本周", "2", UIKeyModifierFlags.Command, "ciShowWeekPage:"),
                CreateScheduleCommand("更多", "3", UIKeyModifierFlags.Command, "ciShowMorePage:"),
                CreateScheduleCommand("显示或隐藏侧栏", "s", UIKeyModifierFlags.Command | UIKeyModifierFlags.Control,
                    "ciToggleSidebar:")
            ]), UIMenuIdentifier.View.GetConstant()!);
    }

    private static UIKeyCommand CreateScheduleCommand(
        string title, string input, UIKeyModifierFlags modifiers, string selector) =>
        UIKeyCommand.Create(title, null, new Selector(selector), input, modifiers, null);

    [Export("ciToday:")]
    public void ShowToday(NSObject sender) => RequestNavigation(new Uri("classisland://app/system/today"));

    [Export("ciPreviousDay:")]
    public void ShowPreviousDay(NSObject sender) => RequestNavigation(new Uri("classisland://app/system/previous-day"));

    [Export("ciNextDay:")]
    public void ShowNextDay(NSObject sender) => RequestNavigation(new Uri("classisland://app/system/next-day"));

    [Export("ciProfile:")]
    public void ShowProfile(NSObject sender) => RequestNavigation(new Uri("classisland://app/system/profile"));

    [Export("ciSettings:")]
    public void ShowSettings(NSObject sender) => RequestNavigation(new Uri("classisland://app/system/settings"));

    [Export("ciShowTodayPage:")]
    public void ShowTodayPage(NSObject sender) => RunMainViewCommand(view => view.ShowPageAsync("classisland.today"));

    [Export("ciShowWeekPage:")]
    public void ShowWeekPage(NSObject sender) => RunMainViewCommand(view => view.ShowPageAsync("classisland.week"));

    [Export("ciShowMorePage:")]
    public void ShowMorePage(NSObject sender) => RunMainViewCommand(view => view.ShowPageAsync("classisland.more"));

    [Export("ciToggleSidebar:")]
    public void ToggleSidebar(NSObject sender) => RunMainViewCommand(view =>
    {
        view.ToggleNavigationPane();
        return Task.CompletedTask;
    });

    [Export("ciClassSwap:")]
    public void ShowClassSwap(NSObject sender) => RunMainViewCommand(view =>
    {
        view.OpenClassSwapWindow();
        return Task.CompletedTask;
    });

    [Export("ciTemporaryClassPlan:")]
    public void ShowTemporaryClassPlan(NSObject sender) => RunMainViewCommand(_ =>
    {
        // 与“更多”页的“加载临时课表…”保持一致。
        var window = IAppHost.GetService<ProfileSettingsWindow>();
        window.OpenDrawer("TemporaryClassPlan");
        window.Open();
        return Task.CompletedTask;
    });

    private void RunMainViewCommand(Func<MainView, Task> command)
    {
        if (_isHudProcess || !_isAppNavigationReady || _systemNavigationService is not { } service) return;
        Dispatcher.UIThread.Post(async () => await service.RunMainViewCommandAsync(command));
    }
}
