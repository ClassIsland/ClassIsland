using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using System.Text.Json;
using Action = ClassIsland.Core.Models.Action.Action;

namespace ClassIsland.Services;

public class ActionService(ILogger<ActionService> logger) : IActionService
{
    public ILogger<ActionService> Logger { get; } = logger;

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

    public void InvokeAction(List<Action> actions)
    {
        foreach (var action in actions)
        {
            InvokeAction(action, isBack: false);
        }
    }

    public void InvokeBackAction(List<Action> actions)
    {
        foreach (var action in actions)
        {
            InvokeAction(action, isBack: true);
        }
    }

    private void InvokeAction(Action action, bool isBack = false)
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
            actionRegistryInfo.BackHandle?.Invoke(settings);
        }
        else
        {
            if (actionRegistryInfo.Handle != null)
            {
                actionRegistryInfo.Handle(settings);
            }
            else
            {
                Logger.LogWarning($"行动 {action.Id} 的处理程序没有注册。");
            }
        }
    }
}