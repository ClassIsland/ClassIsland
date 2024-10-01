using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using System.Text.Json;
using Action = ClassIsland.Core.Models.Action.Action;
using ClassIsland.Services.ActionHandlers;
using System.Threading.Tasks;

namespace ClassIsland.Services;

public class ActionService : IActionService
{
    public ActionService(ILogger<ActionService> logger, IRulesetService rulesetService, SettingsService settingsService)
    {
        Logger = logger;
        RulesetService = rulesetService;
        SettingsService = settingsService;

        // TODO: 改为在 App.xaml.cs 中注册 Handler。
        new AppSettingsActionHandler(settingsService, this);
        new SleepActionHandler(this);
        new RunActionHandler(this, App.GetService<ILogger<RunActionHandler>>());

        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        foreach (var p in SettingsService.Settings.RuleActionPairs)
        {
            if (!p.IsEnabled) continue;
            if (RulesetService.IsRulesetSatisfied(p.Ruleset))
            {
                if (p.ActionList.IsOn) continue;
                InvokeActionList(p.ActionList);
                p.ActionList.IsOn = true;
            }
            else
            {
                if (!p.ActionList.IsOn) continue;
                InvokeBackActionList(p.ActionList);
                p.ActionList.IsOn = false;
            }
        }
    }

    public ILogger<ActionService> Logger { get; }
    public IRulesetService RulesetService { get; }
    public SettingsService SettingsService { get; }

    public void RegisterActionHandler(string id, ActionRegistryInfo.HandleDelegate handler)
    {
        if (!IActionService.Actions.TryGetValue(id, out var actionRegistryInfo))
            throw new KeyNotFoundException($"找不到行动 {id}。");

        actionRegistryInfo.Handle += handler;
    }

    public void RegisterActionBackHandler(string id, ActionRegistryInfo.HandleDelegate handler)
    {
        if (!IActionService.Actions.TryGetValue(id, out var actionRegistryInfo))
            throw new KeyNotFoundException($"找不到行动 {id}。");

        actionRegistryInfo.BackHandle += handler;
    }

    public void DebugInvokeActionListSync(ActionList actionList)
    {
            foreach (var action in actionList.Actions)
            {
                InvokeAction(action, actionList.Guid);
            }
    }

    public void InvokeActionList(ActionList actionList)
    {
        Task.Run(() =>
        {
            foreach (var action in actionList.Actions)
            {
                InvokeAction(action, actionList.Guid);
            }
        });
    }

    public void InvokeBackActionList(ActionList actionList)
    {
        Task.Run(() =>
        {
            foreach (var action in actionList.Actions)
            {
                InvokeAction(action, actionList.Guid, isBack: true);
            }
        });
    }

    void InvokeAction(Action action, string guid, bool isBack = false)
    {
        if (!IActionService.Actions.TryGetValue(action.Id, out var actionRegistryInfo))
        {
            Logger.LogWarning($"找不到行动 {action.Id} 的注册信息。");
            return;
        }

        object? settings = null;
        var settingsType = actionRegistryInfo.SettingsType;
        if (settingsType != null)
        {
            settings = action.Settings ?? Activator.CreateInstance(settingsType);
            if (settings is JsonElement json)
            {
                settings = json.Deserialize(settingsType);
            }
        }
        if (isBack)
        {
            actionRegistryInfo.BackHandle?.Invoke(settings, guid);
            Logger.LogTrace($"触发恢复行动 {action.Id}（{IActionService.Actions[action.Id].Name}）。");
        }
        else
        {
            if (actionRegistryInfo.Handle != null)
            {
                Logger.LogTrace($"触发行动 {action.Id}（{IActionService.Actions[action.Id].Name}）。");
                actionRegistryInfo.Handle(settings, guid);
            }
            else
            {
                Logger.LogWarning($"行动 {action.Id} 的处理程序没有注册。");
            }
        }
    }
}