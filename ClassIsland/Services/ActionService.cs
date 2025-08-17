using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using System.Threading.Tasks;
using AvaloniaEdit.Utils;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Models.Automation;
using ClassIsland.Models.Actions;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Automation;
using static ClassIsland.Core.Abstractions.Services.IActionService;
namespace ClassIsland.Services;

/// <inheritdoc />
public class ActionService : IActionService
{
    public async Task InvokeActionSetAsync(ActionSet actionSet, bool isRevertable = true)
    {
        if (actionSet.Status == ActionSetStatus.Reverting)
            await InterruptActionSetAsync(actionSet);
        if (actionSet.IsWorking) return;

        actionSet.SetStartRunning(true);
        try
        {
            await ChangeableListForEachAsync(
                () => actionSet.ActionItems.Where(x => !x.IsRevertActionItem),
                async Task (x) =>
                {
                    await InvokeActionItemAsync(x, actionSet, isRevertable && actionSet.IsRevertEnabled);
                },
                actionSet.InterruptCts?.Token);
        }
        finally
        {
            actionSet.SetEndRunning(true);
        }
    }

    public async Task RevertActionSetAsync(ActionSet actionSet)
    {
        if (actionSet.Status == ActionSetStatus.Invoking)
            await InterruptActionSetAsync(actionSet);
        if (actionSet.IsWorking) return;

        actionSet.SetStartRunning(false);
        try
        {
            await ChangeableListForEachAsync(
                () => actionSet.ActionItems.Where(x => x.IsRevertActionItem || x.IsRevertEnabled),
                async Task(x) =>
                {
                    if (x.IsRevertActionItem)
                        await InvokeActionItemAsync(x, actionSet, isRevertable: false);
                    else if (x.IsRevertEnabled)
                        await RevertActionItemAsync(x, actionSet);
                },
                actionSet.InterruptCts?.Token);
        }
        finally
        {
            actionSet.SetEndRunning(false);
        }
    }

    public async Task InterruptActionSetAsync(ActionSet actionSet)
    {
        if (actionSet.InterruptCts != null)
            await actionSet.InterruptCts.CancelAsync();
        if (actionSet.RunningTcs != null)
            await actionSet.RunningTcs.Task;
    }

    public async Task InvokeActionItemAsync(ActionItem actionItem, ActionSet actionSet, bool isRevertable = true)
    {
        if (actionSet.InterruptCts?.Token.IsCancellationRequested != false) return;
        var provider = ActionBase.GetInstance(actionItem);
        if (provider == null) return;
        if (!ActionInfos.TryGetValue(actionItem.Id, out var actionInfo)) return;

        var actionItemText = $"行动组“{actionSet.Name}”中的{(actionItem.IsRevertActionItem ? "恢复" : "")}行动项“{actionInfo.Name}”";
        Logger.LogTrace("触发{行动项}。", actionItemText);
        try
        {
            await provider.InvokeAsync(actionItem, actionSet, isRevertable && actionItem.IsRevertEnabled);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "触发{行动项}时出现错误。", actionItemText);
        }
    }

    public async Task RevertActionItemAsync(ActionItem actionItem, ActionSet actionSet)
    {
        if (actionItem.IsRevertActionItem || (!actionItem.IsRevertEnabled && false) ||
            actionSet.InterruptCts?.Token.IsCancellationRequested != false)
            return;
        var provider = ActionBase.GetInstance(actionItem);
        if (provider == null) return;
        if (!ActionInfos.TryGetValue(actionItem.Id, out var actionInfo) || actionInfo.IsRevertable == false) return;

        var actionItemText = $"行动组“{actionSet.Name}”中的行动项“{actionInfo.Name}”";
        Logger.LogTrace("恢复{行动项}。", actionItemText);
        try
        {
            await provider.RevertAsync(actionItem, actionSet);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "恢复{行动项}时出现错误。", actionItemText);
        }
    }

    static async Task ChangeableListForEachAsync<TItem>(
        Func<IEnumerable<TItem>> listProvider,
        Func<TItem, Task> action,
        CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested == true) return;
        var currentList = listProvider().ToList();

        var i = 0;
        while (true)
        {
            if (i >= currentList.Count) break;

            var item = currentList[i];
            await action(item);

            if (cancellationToken?.IsCancellationRequested == true) break;

            currentList = listProvider().ToList();
            var newIndex = currentList.IndexOf(item);

            if (newIndex == -1) continue;
            i = newIndex + 1;
        }
    }

    readonly ILogger<ActionService> Logger;

    /// <inheritdoc cref="IActionService" />
    public ActionService(ILogger<ActionService> logger)
    {
        Logger = logger;

        ActionMenuTree.Add(new ActionMenuTreeGroup("运行", "\uec2e"));
        ActionMenuTree["运行"].AddRange([
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "应用程序", "\uF4B1",
                s => s.RunType = RunActionSettings.RunActionRunType.Application),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run",
                OperatingSystem.IsWindows() ? "cmd 命令" : "终端命令",
                "\uE508",
                s => s.RunType = RunActionSettings.RunActionRunType.Command),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "文件", "\uE687",
                s => s.RunType = RunActionSettings.RunActionRunType.File),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "文件夹", "\uE875",
                s => s.RunType = RunActionSettings.RunActionRunType.Folder),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "Url 链接", "\uE905",
                s => s.RunType = RunActionSettings.RunActionRunType.Url),
        ]);

        ActionMenuTree.Add(new ActionMenuTreeGroup("提醒", "\ue025"));
        ActionMenuTree["提醒"].AddRange([
            new ActionMenuTreeItem<NotificationActionSettings>("classisland.showNotification", "显示提醒…", "\ue02f",
                s => s.IsWaitForCompleteEnabled = false),
            new ActionMenuTreeItem<NotificationActionSettings>("classisland.showNotification", "显示提醒并等待…", "\ue02b",
                s => s.IsWaitForCompleteEnabled = true),
            new ActionMenuTreeItem<WeatherNotificationActionSettings>("classisland.notification.weather", "三天天气预报", "\ue4dc",
                s => s.NotificationKind = 0),
            new ActionMenuTreeItem<WeatherNotificationActionSettings>("classisland.notification.weather", "气象预警", "\uf431",
                s => s.NotificationKind = 1),
            new ActionMenuTreeItem<WeatherNotificationActionSettings>("classisland.notification.weather", "逐小时天气预报", "\uf357",
                s => s.NotificationKind = 2),
        ]);

        ActionMenuTree.Add(new ActionMenuTreeGroup("ClassIsland", "\ue454"));
        ActionMenuTree["ClassIsland"].AddRange([
            new ActionMenuTreeItem("classisland.app.quit", "退出 ClassIsland", "\ue0df"),
            new ActionMenuTreeItem("classisland.app.restart", "重启 ClassIsland", "\ue0bd"),
        ]);
    }

    static void AsTypeOrNew<T>(ref object? obj) where T : new()
    {
        obj = obj is T t ? t : new T();
    }

    [Obsolete("注意！行动 v2 注册方法已过时，请参阅 ClassIsland 开发文档进行迁移。")]
    public void RegisterActionHandler(string id, Action<object, string> i2)
    {
        var i1 = ObsoleteActionHandlers[id].Item1;
        var i3 = ObsoleteActionHandlers[id].Item3;
        ObsoleteActionHandlers[id] = (i1, i2, i3);
    }

    [Obsolete("注意！行动 v2 注册方法已过时，请参阅 ClassIsland 开发文档进行迁移。")]
    public void RegisterRevertHandler(string id, Action<object, string> i3)
    {
        var i1 = ObsoleteActionHandlers[id].Item1;
        var i2 = ObsoleteActionHandlers[id].Item2;
        ObsoleteActionHandlers[id] = (i1, i2, i3);
    }
}