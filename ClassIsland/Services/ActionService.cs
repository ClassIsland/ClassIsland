using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using System.Text.Json;
using Action = ClassIsland.Shared.Models.Action.Action;
using System.Threading.Tasks;
using ClassIsland.Shared.Models.Action;

namespace ClassIsland.Services;

public class ActionService : IActionService
{
    private ILogger<ActionService> Logger { get; }

    private DateTime LastActionRunTime { get; set; } 

    public ActionService(ILogger<ActionService> logger, ILessonsService lessonsService, IExactTimeService exactTimeService, IProfileService profileService)
    {
        Logger = logger;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        ProfileService = profileService;

        LastActionRunTime = ExactTimeService.GetCurrentLocalDateTime();
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        if (!ProfileService.IsCurrentProfileTrusted)
        {
            return;
        }
        var currentTime = ExactTimeService.GetCurrentLocalDateTime();
        var triggeredActions = LessonsService.CurrentClassPlan?.TimeLayout?.Layouts
            .Where(x => x.TimeType == 3 && x.StartTime > LastActionRunTime.TimeOfDay &&
                        x.StartTime <= currentTime.TimeOfDay)
            .Select(x => x)
            .ToList();
        LastActionRunTime = currentTime;
        if (triggeredActions == null)
        {
            return;
        }

        foreach (var i in triggeredActions)
        {
            if (i.ActionSet == null)
            {
                continue;
            }
            Logger.LogInformation("触发时间点行动：{}/[{}]", LessonsService.CurrentClassPlan?.TimeLayout?.Name, i.StartTime);
            Invoke(i.ActionSet);
        }
    }

    public ILessonsService LessonsService { get; }
    public IExactTimeService ExactTimeService { get; }
    public IProfileService ProfileService { get; }

    public void RegisterActionHandler(string id, ActionRegistryInfo.HandleDelegate handler)
    {
        // TODO: stub
        // return;
        if (!IActionService.Actions.TryGetValue(id, out var actionRegistryInfo))
            throw new KeyNotFoundException($"找不到行动 {id}。");

        actionRegistryInfo.Handle += handler;
        Logger.LogTrace($"注册行动：{id}（{IActionService.Actions[id].Name}）");
    }

    public void RegisterRevertHandler(string id, ActionRegistryInfo.HandleDelegate handler)
    {
        // return;
        if (!IActionService.Actions.TryGetValue(id, out var actionRegistryInfo))
            throw new KeyNotFoundException($"找不到行动 {id}。");

        actionRegistryInfo.RevertHandle += handler;
        Logger.LogTrace($"注册恢复行动：{id}（{IActionService.Actions[id].Name}）");
    }

    public void Invoke(ActionSet actionSet)
    {
        if (App.ApplicationCommand.Safe)
        {
            return;
        }
        foreach (var action in actionSet.Actions)
            action.Exception = null;
        if (actionSet.IsRevertEnabled)
        {
            actionSet.IsOn = true;
        }
        Task.Run(() =>
        {
            foreach (var action in actionSet.Actions)
            {
                InvokeAction(action, actionSet.Guid);
            }
        });
    }

    public void Revert(ActionSet actionSet)
    {
        if (App.ApplicationCommand.Safe)
        {
            return;
        }
        foreach (var action in actionSet.Actions)
            action.Exception = null;
        actionSet.IsOn = false;
        Task.Run(() =>
        {
            foreach (var action in actionSet.Actions)
            {
                InvokeAction(action, actionSet.Guid, isBack: true);
            }
        });
    }

    void InvokeAction(Action action, string guid, bool isBack = false)
    {
        if (action.Id == string.Empty) return;
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
            if (actionRegistryInfo.RevertHandle != null)
            {
                actionRegistryInfo.RevertHandle.Invoke(settings, guid);
                Logger.LogTrace($"恢复行动：{action.Id}（{IActionService.Actions[action.Id].Name}）");
            }
        }
        else
        {
            if (actionRegistryInfo.Handle != null)
            {
                Logger.LogTrace($"触发行动：{action.Id}（{IActionService.Actions[action.Id].Name}）");
                action.IsWorking = true;
                try
                {
                    actionRegistryInfo.Handle(settings, guid);
                }
                catch (Exception ex)
                {
                    action.Exception = ex;
                }
                finally
                {
                    action.IsWorking = false;
                }
            }
            else
            {
                Logger.LogWarning($"行动 {action.Id}（{IActionService.Actions[action.Id].Name}）的处理程序没有注册。");
            }
        }
    }



    public bool ExistRevertHandler(Action action)
    {
        if (action.Id == string.Empty) return false;
        if (!IActionService.Actions.TryGetValue(action.Id, out var actionRegistryInfo))
        {
            return false;
        }
        if (actionRegistryInfo.RevertHandle != null)
        {
            return true;
        }
        return false;
    }
}