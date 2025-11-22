using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        MigrateActionSet(actionSet);

        Logger.LogInformation("触发行动组“{行动组}”。", actionSet.Name);
        actionSet.SetStartRunning(true);
        try
        {
            await Task.Run(
                    async () =>
                        await ChangeableListForEachAsync(
                            () => actionSet.ActionItems.Where(x => !x.IsRevertActionItem),
                            async x =>
                            {
                                await InvokeActionItemAsync(x, actionSet, isRevertable && actionSet.IsRevertEnabled);
                            },
                            actionSet.InterruptCts?.Token),
                    actionSet.InterruptCts?.Token ?? CancellationToken.None)
                .ConfigureAwait(false);
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

        MigrateActionSet(actionSet);

        Logger.LogInformation("恢复行动组“{行动组}”。", actionSet.Name);
        actionSet.SetStartRunning(false);
        try
        {
            await Task.Run(
                    async () =>
                        await ChangeableListForEachAsync(
                            () => actionSet.ActionItems.Where(x => x.IsRevertActionItem || x.IsRevertEnabled),
                            async x =>
                            {
                                if (x.IsRevertActionItem)
                                    await InvokeActionItemAsync(x, actionSet, isRevertable: false);
                                else if (x.IsRevertEnabled)
                                    await RevertActionItemAsync(x, actionSet);
                            },
                            actionSet.InterruptCts?.Token),
                    actionSet.InterruptCts?.Token ?? CancellationToken.None)
                .ConfigureAwait(false);
        }
        finally
        {
            actionSet.SetEndRunning(false);
        }
    }

    public async Task InterruptActionSetAsync(ActionSet actionSet)
    {
        Logger.LogInformation("中断运行行动组“{行动组}”。", actionSet.Name);
        try
        {
            if (actionSet.InterruptCts != null)
                await actionSet.InterruptCts.CancelAsync();
            if (actionSet.RunningTcs != null)
                await actionSet.RunningTcs.Task;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "中断运行行动组“{行动组}”时出现错误。", actionSet.Name);
        }
    }

    public async Task InvokeActionItemAsync(ActionItem actionItem, ActionSet actionSet, bool isRevertable = true)
    {
        if (actionSet.InterruptCts?.Token.IsCancellationRequested != false) return;
        var provider = ActionBase.GetInstance(actionItem);
        if (provider == null) return;
        if (!ActionInfos.TryGetValue(actionItem.Id, out var actionInfo)) return;

        MigrateActionItem(actionItem);

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
        if (actionItem.IsRevertActionItem || !actionItem.IsRevertEnabled ||
            actionSet.InterruptCts?.Token.IsCancellationRequested != false)
            return;
        var provider = ActionBase.GetInstance(actionItem);
        if (provider == null) return;
        if (!ActionInfos.TryGetValue(actionItem.Id, out var actionInfo) || !actionInfo.IsRevertable) return;

        MigrateActionItem(actionItem);

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

    public void MigrateActionSet(ActionSet actionSet) { }

    public void MigrateActionItem(ActionItem actionItem) { }

    public void MigrateUnknownActionItem(ActionItem actionItem)
    {
        // 1.7.107.0
        // 迁移应用设置行动 classisland.settings
        {
            const string prefix = "classisland.settings.";
            if (actionItem.Id.StartsWith(prefix))
            {
                Logger.LogInformation("迁移行动项 {} 到新应用设置行动", actionItem.Id);

                var suffix = actionItem.Id[prefix.Length..];
                var processedName = $"{char.ToUpper(suffix[0])}{suffix[1..]}";

                actionItem.Id = "classisland.settings";

                var settings = new ModifyAppSettingsActionSettings { Name = processedName };
                if (actionItem.Settings is JsonElement jsonElement &&
                    jsonElement.TryGetProperty("Value", out var value))
                {
                    settings.Value = value.ToString();
                }
                actionItem.Settings = settings;
            }
        }
    }

    readonly ILogger<ActionService> Logger;

    /// <inheritdoc cref="IActionService" />
    public ActionService(ILogger<ActionService> logger)
    {
        Logger = logger;
        ActionMenuTree.Add(
            new ActionMenuTreeGroup("应用设置", "\uef27",
            SettingItem("选择应用设置…", "\ue454", ""),
            SettingItem("组件配置方案", "\ue06f", "CurrentComponentConfig"),
            SettingItem("应用主题", "\ue5cb", "Theme"),
            SettingItem("窗口停靠位置", "\uf397", "WindowDockingLocation")
        ));


        ActionMenuTree.Add(
            new ActionMenuTreeGroup("运行", "\uec2e",
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "应用程序", "\uf4b1",
                s => s.RunType = RunActionSettings.RunActionRunType.Application),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run",
                OperatingSystem.IsWindows() ? "cmd 命令" : "终端命令",
                "\ue508",
                s => s.RunType = RunActionSettings.RunActionRunType.Command),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "文件", "\ue687",
                s => s.RunType = RunActionSettings.RunActionRunType.File),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "文件夹", "\ue875",
                s => s.RunType = RunActionSettings.RunActionRunType.Folder),
            new ActionMenuTreeItem<RunActionSettings>("classisland.os.run", "Url 链接", "\ue905",
                s => s.RunType = RunActionSettings.RunActionRunType.Url)
        ));

        ActionMenuTree.Add(
            new ActionMenuTreeGroup("提醒", "\ue025",
            new ActionMenuTreeItem<NotificationActionSettings>("classisland.showNotification", "显示提醒…", "\ue02f",
                s => s.IsWaitForCompleteEnabled = false),
            new ActionMenuTreeItem<NotificationActionSettings>("classisland.showNotification", "显示提醒并等待…", "\ue02b",
                s => s.IsWaitForCompleteEnabled = true),
            new ActionMenuTreeItem<WeatherNotificationActionSettings>("classisland.notification.weather", "三天天气预报", "\ue4dc",
                s => s.NotificationKind = 0),
            new ActionMenuTreeItem<WeatherNotificationActionSettings>("classisland.notification.weather", "气象预警", "\uf431",
                s => s.NotificationKind = 1),
            new ActionMenuTreeItem<WeatherNotificationActionSettings>("classisland.notification.weather", "逐小时天气预报", "\uf357",
                s => s.NotificationKind = 2)
        ));

        ActionMenuTree.Add(
            new ActionMenuTreeGroup("ClassIsland", "\ue454",
            new ActionMenuTreeItem("classisland.app.quit", "退出 ClassIsland", "\ue0df"),
            new ActionMenuTreeItem("classisland.app.restart", "重启 ClassIsland", "\ue0bd")
        ));

        return;

        static ActionMenuTreeItem<ModifyAppSettingsActionSettings> SettingItem(string displayName, string icon, string name) =>
            new("classisland.settings", displayName, icon, s => s.Name = name);
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