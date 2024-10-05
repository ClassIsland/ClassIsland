using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using System.Text.Json;
using Action = ClassIsland.Core.Models.Action.Action;
using System.Threading.Tasks;
namespace ClassIsland.Services;

public class ActionService(ILogger<ActionService> Logger) : IActionService
{
    public void RegisterActionHandler(string id, ActionRegistryInfo.HandleDelegate handler)
    {
        if (!IActionService.Actions.TryGetValue(id, out var actionRegistryInfo))
            throw new KeyNotFoundException($"找不到行动 {id}。");

        actionRegistryInfo.Handle += handler;
        Logger.LogTrace($"注册行动：{id}（{IActionService.Actions[id].Name}）");
    }

    public void RegisterRevertHandler(string id, ActionRegistryInfo.HandleDelegate handler)
    {
        if (!IActionService.Actions.TryGetValue(id, out var actionRegistryInfo))
            throw new KeyNotFoundException($"找不到行动 {id}。");

        actionRegistryInfo.RevertHandle += handler;
        Logger.LogTrace($"注册恢复行动：{id}（{IActionService.Actions[id].Name}）");
    }

    public void Invoke(Actionset actionset)
    {
        foreach (var action in actionset.Actions)
            action.Exception = null;
        Task.Run(() =>
        {
            foreach (var action in actionset.Actions)
            {
                InvokeAction(action, actionset.Guid);
            }
        });
    }

    public void Revert(Actionset actionset)
    {
        foreach (var action in actionset.Actions)
            action.Exception = null;
        Task.Run(() =>
        {
            foreach (var action in actionset.Actions)
            {
                InvokeAction(action, actionset.Guid, isBack: true);
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