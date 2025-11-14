using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using System.Text.Json;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Models.Automation;
using ClassIsland.Models;
using ClassIsland.Models.Actions;
using Microsoft.Extensions.DependencyInjection;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Automation;
namespace ClassIsland.Services;

/// <inheritdoc cref="IAutomationService"/>
public class AutomationService(ILogger<AutomationService> Logger, IRulesetService RulesetService, SettingsService SettingsService,
    IActionService ActionService, IWindowRuleService WindowRuleService, IProfileService ProfileService, ILessonsService LessonsService,
    IExactTimeService ExactTimeService) : ObservableRecipient, IAutomationService
{
    public static readonly string AutomationConfigsFolderPath =
        Path.Combine(CommonDirectories.AppConfigPath, "Automations");

    public string CurrentConfigPath =>
        Path.GetFullPath(Path.Combine(AutomationConfigsFolderPath,
            SettingsService.Settings.CurrentAutomationConfig + ".json"));

    public void Initialize()
    {
        LoadConfig();
        RefreshConfigs();

        SettingsService.Settings.PropertyChanging += (_, e) =>
        {
            if (e.PropertyName == nameof(Settings.CurrentAutomationConfig))
            {
                InterruptAllWorkflows();
                SaveConfig("切换自动化配置文件。");
            }
        };
        SettingsService.Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Settings.CurrentAutomationConfig))
                LoadConfig();
            else if (e.PropertyName == nameof(Settings.IsAutomationEnabled) &&
                     !SettingsService.Settings.IsAutomationEnabled)
                InterruptAllWorkflows();
        };

        if (App.ApplicationCommand.Safe) return;
        // 注意：以下代码在安全模式下不会运行。

        LastActionRunTime = ExactTimeService.GetCurrentLocalDateTime();
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
    }

#region 时间点行动

    /// <summary>
    /// 时间点行动：自动触发行动。
    /// </summary>
    void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        if (!ProfileService.IsCurrentProfileTrusted) return;

        var currentTime = ExactTimeService.GetCurrentLocalDateTime();
        var triggerActions = LessonsService.CurrentClassPlan?.TimeLayout?.Layouts
            .Where(x => x.TimeType == 3 && x.StartTime > LastActionRunTime.TimeOfDay &&
                        x.StartTime <= currentTime.TimeOfDay)
            .ToList();
        LastActionRunTime = currentTime;
        if (triggerActions == null) return;

        foreach (var i in triggerActions)
        {
            if (i.ActionSet == null) continue;

            Logger.LogInformation("触发时间点行动：{}/[{}]", LessonsService.CurrentClassPlan?.TimeLayout?.Name, i.StartTime);
            ActionService.InvokeActionSetAsync(i.ActionSet, false);
        }
    }

#endregion

#region 自动化工作流

    /// <summary>
    /// 自动化工作流：触发器触发，（规则集满足），触发行动。
    /// </summary>
    void TriggerTriggered(object? sender, EventArgs e)
    {
        if (!SettingsService.Settings.IsAutomationEnabled) return;
        if (sender is not TriggerBase trigger) return;
        var workflow = trigger.AssociatedWorkflow;

        if (!workflow.ActionSet.IsEnabled) return;
        if (workflow.ActionSet.IsRevertEnabled && workflow.ActionSet.Status != ActionSetStatus.Normal)
        {
            return;
        }

        Logger.LogTrace("工作流 {} 由触发器 {} 触发", workflow.ActionSet.Name, trigger);
        if (workflow.IsConditionEnabled && !RulesetService.IsRulesetSatisfied(workflow.Ruleset))
            return;
        ActionService.InvokeActionSetAsync(workflow.ActionSet);
    }

    /// <summary>
    /// 自动化工作流：触发器恢复，恢复行动。
    /// </summary>
    void TriggerTriggeredRevert(object? sender, EventArgs e)
    {
        if (!SettingsService.Settings.IsAutomationEnabled) return;
        if (sender is not TriggerBase trigger) return;
        var workflow = trigger.AssociatedWorkflow;

        if (workflow.ActionSet.Status != ActionSetStatus.IsOn)
        {
            return;
        }

        Logger.LogTrace("工作流 {} 由触发器 {} 触发恢复", workflow.ActionSet.Name, trigger);
        ActionService.RevertActionSetAsync(workflow.ActionSet);
    }

    /// <summary>
    /// 自动化工作流：规则集不再满足，恢复行动。
    /// </summary>
    void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        if (!SettingsService.Settings.IsAutomationEnabled) return;

        foreach (var workflow in Workflows.Where(x => x is
                     { ActionSet: { Status: ActionSetStatus.IsOn, IsRevertEnabled: true }, IsConditionEnabled: true }))
        {
            if (RulesetService.IsRulesetSatisfied(workflow.Ruleset) || workflow.ActionSet.Status != ActionSetStatus.IsOn)
                continue;
            ActionService.RevertActionSetAsync(workflow.ActionSet);
        }
    }

#endregion

#region 配置文件

    void LoadConfig()
    {
        // 释放当前加载的工作流
        foreach (var i in Workflows)
        {
            UnloadWorkflow(i);
        }

        Workflows.CollectionChanged -= WorkflowsOnCollectionChanged;

        if (File.Exists(CurrentConfigPath))
        {
            Workflows = ConfigureFileHelper.LoadConfig<ObservableCollection<Workflow>>(CurrentConfigPath);

            // migrate
            {
                const string prefix = "classisland.settings.";
                foreach (var item in Workflows
                             .SelectMany(workflow => workflow.ActionSet.ActionItems)
                             .Where(item => item.Id.StartsWith(prefix)))
                {
                    var suffix = item.Id[prefix.Length..];
                    var processedName = $"{char.ToUpper(suffix[0])}{suffix[1..]}";
                    var s = new ModifyAppSettingsActionSettings
                    {
                        Name = processedName,
                        Value = item.Settings.GetType().GetProperty("Value")?.GetValue(item.Settings)
                    };
                    item.Settings = s;
                    item.Id = "classisland.settings";
                }
            }
        }
        else
        {
            Workflows = ConfigureFileHelper.CopyObject(new ObservableCollection<Workflow>());
            SaveConfig("新建自动化配置");
        }

        if (App.ApplicationCommand.Safe) return;
        // 注意：以下代码在安全模式下不会运行。

        foreach (var i in Workflows)
        {
            LoadWorkflow(i);
        }

        Workflows.CollectionChanged += WorkflowsOnCollectionChanged;
    }

    public void SaveConfig(string note = "")
    {
        Logger.LogDebug(note == "" ? "写入自动化配置（{}.json）" : "写入自动化配置（{}.json）：{note}",
            SettingsService.Settings.CurrentAutomationConfig, note);
        ConfigureFileHelper.SaveConfig(CurrentConfigPath, Workflows);
    }

    public void RefreshConfigs()
    {
        Configs = Directory.GetFiles(AutomationConfigsFolderPath, "*.json")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .ToList();
    }

    void ActionSetOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ActionSet actionSet) return;
        if (e.PropertyName == nameof(ActionSet.Status) && !actionSet.IsWorking)
            SaveConfig($"行动组“{actionSet.Name}”的状态改变。");
    }

    void InterruptAllWorkflows()
    {
        foreach (var x in Workflows)
        {
            ActionService.InterruptActionSetAsync(x.ActionSet);
        }
    }

#endregion

#region 加载工作流

    void WorkflowsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (Workflow workflow in e.NewItems!)
                {
                    LoadWorkflow(workflow);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (Workflow workflow in e.OldItems!)
                {
                    UnloadWorkflow(workflow);
                }

                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void UnloadWorkflow(Workflow workflow)
    {
        // Logger.LogInformation("卸载工作流 {}", workflow.ActionSet.Name);
        workflow.Unload();
        foreach (var trigger in workflow.Triggers)
        {
            UnloadTrigger(workflow, trigger);
        }

        Logger.LogDebug("成功卸载工作流 {}", workflow.ActionSet.Name);
    }

    void LoadWorkflow(Workflow workflow)
    {
        // Logger.LogInformation("加载工作流 {}", workflow.ActionSet.Name);
        workflow.Triggers.CollectionChanged += TriggersOnCollectionChanged;
        workflow.ActionSet.PropertyChanged += ActionSetOnPropertyChanged;
        workflow.Unloading += WorkflowOnUnloading;

        foreach (var trigger in workflow.Triggers)
        {
            LoadTrigger(workflow, trigger);
        }

        Logger.LogDebug("成功加载工作流 {}", workflow.ActionSet.Name);
        return;

        void WorkflowOnUnloading(object? sender, EventArgs e)
        {
            workflow.Unloading -= WorkflowOnUnloading;
            workflow.ActionSet.PropertyChanged -= ActionSetOnPropertyChanged;
            workflow.Triggers.CollectionChanged -= TriggersOnCollectionChanged;
        }

        void TriggersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (TriggerSettings trigger in e.NewItems!)
                    {
                        LoadTrigger(workflow, trigger);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (TriggerSettings trigger in e.OldItems!)
                    {
                        UnloadTrigger(workflow, trigger);
                    }

                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

#endregion

#region 加载触发器

    void LoadTrigger(Workflow workflow, TriggerSettings trigger, bool registerUnloading = true)
    {
        if (trigger.TriggerInstance != null)
        {
            return;
        }

        // Logger.LogDebug("加载触发器 {}/{}", workflow.ActionSet.Name, trigger.Id);
        var settings = trigger.Settings;
        trigger.TriggerInstance = ActivateTrigger(trigger.AssociatedTriggerInfo, ref settings);
        trigger.Settings = settings;
        trigger.PropertyChanged += TriggerOnPropertyChanged;
        trigger.Unloading += TriggerOnUnloading;
        if (trigger.TriggerInstance == null)
        {
            return;
        }

        trigger.TriggerInstance.AssociatedWorkflow = workflow;
        trigger.TriggerInstance.Triggered += TriggerTriggered;
        trigger.TriggerInstance.TriggeredRevert += TriggerTriggeredRevert;
        trigger.TriggerInstance.Loaded();

        Logger.LogDebug("成功加载触发器 {}/{}", workflow.ActionSet.Name, trigger.Id);
        return;

        void TriggerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(trigger.Id))
            {
                return;
            }

            UnloadTrigger(workflow, trigger);
            LoadTrigger(workflow, trigger, false);
        }

        void TriggerOnUnloading(object? sender, EventArgs e)
        {
            trigger.Unloading -= TriggerOnUnloading;
            trigger.PropertyChanged -= TriggerOnPropertyChanged;
        }
    }

    void UnloadTrigger(Workflow workflow, TriggerSettings trigger)
    {
        if (trigger.TriggerInstance == null)
        {
            return;
        }

        // Logger.LogDebug("卸载触发器 {}/{}", workflow.ActionSet.Name, trigger.Id);
        trigger.Unload();
        trigger.TriggerInstance.UnLoaded();
        trigger.TriggerInstance.Triggered -= TriggerTriggered;
        trigger.TriggerInstance.TriggeredRevert -= TriggerTriggeredRevert;
        trigger.TriggerInstance = null;
        Logger.LogDebug("成功卸载触发器 {}/{}", workflow.ActionSet.Name, trigger.Id);
    }

    /// <summary>
    /// 准备触发器实例。
    /// </summary>
    private static TriggerBase? ActivateTrigger(TriggerInfo? info, ref object? settings)
    {
        if (info == null)
        {
            return null;
        }

        var trigger = IAppHost.Host?.Services.GetKeyedService<TriggerBase>(info.Id);
        if (trigger == null)
        {
            return null;
        }

        if (info.SettingsControlType == null)
        {
            return trigger;
        }

        var baseType = info.SettingsControlType.BaseType;
        if (baseType?.GetGenericArguments().Length > 0)
        {
            var settingsType = baseType.GetGenericArguments().First();
            var settingsReal = settings ?? Activator.CreateInstance(settingsType);
            if (settingsReal is JsonElement json)
            {
                settingsReal = json.Deserialize(settingsType);
            }

            if (settingsReal?.GetType() != settingsType)
            {
                settingsReal = Activator.CreateInstance(settingsType);
            }

            settings = settingsReal;

            trigger.SettingsInternal = settingsReal;
        }

        return trigger;
    }

#endregion

    ObservableCollection<Workflow> _workflows = [];
    public ObservableCollection<Workflow> Workflows
    {
        get => _workflows;
        set
        {
            if (Equals(value, _workflows)) return;
            _workflows = value;
            OnPropertyChanged();
        }
    }

    IReadOnlyList<string> _configs = [];
    public IReadOnlyList<string> Configs
    {
        get => _configs;
        set
        {
            if (Equals(value, _configs)) return;
            _configs = value;
            OnPropertyChanged();
        }
    }

    DateTime LastActionRunTime { get; set; }
}