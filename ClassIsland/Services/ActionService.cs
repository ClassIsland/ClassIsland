using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Action;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Services;

public sealed class ActionService : IActionService
{
    public ActionService(ILogger<ActionService> logger, ILessonsService lessonsService, IExactTimeService exactTimeService, IProfileService profileService)
    {
        Logger = logger;
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        ProfileService = profileService;
        
        // if (App.ApplicationCommand.Safe) return;
        //
        // LastActionRunTime = ExactTimeService.GetCurrentLocalDateTime();
        // LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
    }

    // private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    // {
    //     if (!ProfileService.IsCurrentProfileTrusted) return;
    //     
    //     var currentTime = ExactTimeService.GetCurrentLocalDateTime();
    //     var triggeredActions = LessonsService.CurrentClassPlan?.TimeLayout?.Layouts
    //         .Where(x => x.TimeType == 3 && x.StartTime > LastActionRunTime.TimeOfDay &&
    //                     x.StartTime <= currentTime.TimeOfDay)
    //         .Select(x => x)
    //         .ToList();
    //     LastActionRunTime = currentTime;
    //     if (triggeredActions == null) return;
    //
    //     foreach (var i in triggeredActions)
    //     {
    //         if (i.ActionSet == null) continue;
    //         
    //         Logger.LogInformation("触发时间点行动：{}/[{}]", LessonsService.CurrentClassPlan?.TimeLayout?.Name, i.StartTime);
    //         InvokeActionSetAsync(i.ActionSet, false);
    //     }
    // }
    
    public async Task InvokeActionSetAsync(ActionSet actionSet, bool isRevertable = true)
    {
        actionSet.SetStartRunning(true);
        try
        {
            foreach (var x in actionSet.ActionItems.Where(x => !x.IsRevertActionItem))
            {
                await InvokeActionItemAsync(x, actionSet, isRevertable);
            }
        }
        finally
        {
            actionSet.SetEndRunning(true);
        }
    }
    
    public async Task RevertActionSetAsync(ActionSet actionSet)
    {
        actionSet.SetStartRunning(false);
        try
        {
            foreach (var x in actionSet.ActionItems.Where(x => x.IsRevertActionItem || x.IsRevertEnabled))
            {
                if (x.IsRevertActionItem)
                    await InvokeActionItemAsync(x, actionSet, isRevertable: false);
                else if (x.IsRevertEnabled)
                    await RevertActionItemAsync(x, actionSet);
            }
        }
        finally
        {
            actionSet.SetEndRunning(false);
        }
    }

    public async Task InvokeActionItemAsync(ActionItem actionItem, ActionSet? actionSet = null, bool isRevertable = true)
    {
        if (string.IsNullOrEmpty(actionItem.Id)) return;
        var provider = ActionBase.GetInstance(actionItem);
        if (provider == null) return;
        // if (!IActionService.ActionInfos.TryGetValue(actionItem.Id, out var info)) return;

        var actionItemText = (actionSet != null ? $"行动组“{actionSet.Name}”中的" : "") + (actionItem.IsRevertActionItem ? "恢复" : "") + $"行动项“{actionItem.Id}”";
        Logger.LogTrace("触发{行动项}。", actionItemText);
        try
        {
            await provider.InvokeAsync(actionItem, isRevertable && actionItem.IsRevertEnabled, actionSet?.CancellationTokenSource);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "触发{行动项}时出现错误。", actionItemText);
        }
    }

    public async Task RevertActionItemAsync(ActionItem actionItem, ActionSet? actionSet = null)
    {
        if (string.IsNullOrEmpty(actionItem.Id) || actionItem.IsRevertActionItem || !actionItem.IsRevertEnabled) return;
        var provider = ActionBase.GetInstance(actionItem);
        if (provider == null) return;
        // if (!IActionService.ActionInfos.TryGetValue(actionItem.Id, out var info)) return;
        
        var actionItemText = (actionSet != null ? $"行动组“{actionSet.Name}”中的" : "") + $"行动项“{actionItem.Id}”";
        Logger.LogTrace("恢复{行动项}。", actionItemText);
        try
        {
            await provider.RevertAsync(actionItem, actionSet?.CancellationTokenSource);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "恢复{行动项}时出现错误。", actionItemText);
        }
    }
    
    public ILessonsService LessonsService { get; }
    public IExactTimeService ExactTimeService { get; }
    public IProfileService ProfileService { get; }
    private ILogger<ActionService> Logger { get; }
    private DateTime LastActionRunTime { get; set; } 
}