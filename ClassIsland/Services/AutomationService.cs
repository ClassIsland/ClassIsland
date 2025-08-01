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
using ClassIsland.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Action;

namespace ClassIsland.Services;

public class AutomationService : ObservableRecipient, IAutomationService
{
    public static readonly string AutomationConfigsFolderPath = Path.Combine(CommonDirectories.AppConfigPath, "Automations/");
    public string CurrentConfigPath =>
        Path.GetFullPath(Path.Combine(AutomationConfigsFolderPath, SettingsService.Settings.CurrentAutomationConfig + ".json"));

    public AutomationService(ILogger<AutomationService> logger, IRulesetService rulesetService, SettingsService settingsService, IActionService actionService, IWindowRuleService windowRuleService)
    {
        Logger = logger;
        RulesetService = rulesetService;
        SettingsService = settingsService;
        ActionService = actionService;
        WindowRuleService = windowRuleService;

        RefreshConfigs();
        LoadConfig();
        RefreshConfigs();

        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        
        SettingsService.Settings.PropertyChanging += (_, e) => {
            if (e.PropertyName == nameof(SettingsService.Settings.CurrentAutomationConfig))
                SaveConfig();
        };
        
        SettingsService.Settings.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(SettingsService.Settings.CurrentAutomationConfig))
                LoadConfig();
        };
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        if (!SettingsService.Settings.IsAutomationEnabled) return;

        foreach (var workflow in Workflows.Where(x => x is { ActionSet: { Status: ActionSetStatus.On, IsRevertEnabled: true }, IsConditionEnabled: true }))
        {
            if (RulesetService.IsRulesetSatisfied(workflow.Ruleset)) 
                continue;
            ActionService.RevertActionSetAsync(workflow.ActionSet);
        }
    }

    private void LoadConfig()
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
        }
        else
        {
            Workflows = ConfigureFileHelper.CopyObject(new ObservableCollection<Workflow>());
            SaveConfig();
        }

        foreach (var i in Workflows)
        {
            LoadWorkflow(i);
        }
        Workflows.CollectionChanged += WorkflowsOnCollectionChanged;
    }

    private void WorkflowsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

    private void UnloadWorkflow(Workflow workflow)
    {
        // Logger.LogInformation("卸载工作流 {}", workflow.ActionSet.Name);
        workflow.Unload();
        foreach (var trigger in workflow.Triggers)
        {
            UnloadTrigger(workflow, trigger);
        }

        Logger.LogDebug("成功卸载工作流 {}", workflow.ActionSet.Name);
    }

    private void LoadWorkflow(Workflow workflow)
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

    void ActionSetOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActionSet.Status))
            SaveConfig($"行动组“{(sender as ActionSet)?.Name}”的状态改变。");
        //TODO: 降低保存频率。
    }

    private void LoadTrigger(Workflow workflow, TriggerSettings trigger, bool registerUnloading=true)
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
        trigger.TriggerInstance.TriggeredRecover += TriggerTriggeredRecover;
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

    private void UnloadTrigger(Workflow workflow, TriggerSettings trigger)
    {
        if (trigger.TriggerInstance == null)
        {
            return;
        }
        // Logger.LogDebug("卸载触发器 {}/{}", workflow.ActionSet.Name, trigger.Id);
        trigger.Unload();
        trigger.TriggerInstance.UnLoaded();
        trigger.TriggerInstance.Triggered -= TriggerTriggered;
        trigger.TriggerInstance.TriggeredRecover -= TriggerTriggeredRecover;
        trigger.TriggerInstance = null;
        Logger.LogDebug("成功卸载触发器 {}/{}", workflow.ActionSet.Name, trigger.Id);
    }


    private void TriggerTriggered(object? sender, EventArgs e)
    {
        if (!SettingsService.Settings.IsAutomationEnabled) return;
        if (sender is not TriggerBase trigger) return;
        var workflow = trigger.AssociatedWorkflow;

        if (!workflow.ActionSet.IsEnabled) return;
        if (workflow.ActionSet.IsRevertEnabled && workflow.ActionSet.Status == ActionSetStatus.On)
        {
            return;
        }

        Logger.LogTrace("工作流 {} 由触发器 {} 触发", workflow.ActionSet.Name, trigger);
        if (workflow.IsConditionEnabled && !RulesetService.IsRulesetSatisfied(workflow.Ruleset)) 
            return;
        ActionService.InvokeActionSetAsync(workflow.ActionSet);
        SaveConfig();
    }
    
    private void TriggerTriggeredRecover(object? sender, EventArgs e)
    {
        if (!SettingsService.Settings.IsAutomationEnabled) return;
        if (sender is not TriggerBase trigger) return;
        var workflow = trigger.AssociatedWorkflow;

        if (workflow.ActionSet.Status != ActionSetStatus.On)
        {
            return;
        }
        Logger.LogTrace("工作流 {} 由触发器 {} 触发恢复", workflow.ActionSet.Name, trigger);
        ActionService.RevertActionSetAsync(workflow.ActionSet);
        SaveConfig();
    }

    static TriggerBase? ActivateTrigger(TriggerInfo? info, ref object? settings)
    {
        if (info == null) return null;
        var trigger = IAppHost.Host?.Services.GetKeyedService<TriggerBase>(info.Id);
        if (trigger == null) return null;
        if (info.SettingsControlType == null) return trigger;
        var baseType = info.SettingsControlType.BaseType;
        if (!(baseType?.GetGenericArguments().Length > 0)) return trigger;
        var settingsType = baseType.GetGenericArguments().First();
        if (settings is JsonElement json) settings = json.Deserialize(settingsType);
        if (settings?.GetType() != settingsType) settings = Activator.CreateInstance(settingsType);
        trigger.SettingsInternal = settings;
        return trigger;
    }

    public void SaveConfig(string note = "")
    {
        Logger.LogInformation(note == "" ?
            $"写入自动化配置（{SettingsService.Settings.CurrentAutomationConfig}.json）" :
            $"写入自动化配置（{SettingsService.Settings.CurrentAutomationConfig}.json）：{note}");
        ConfigureFileHelper.SaveConfig(CurrentConfigPath, Workflows);
    }

    public void RefreshConfigs()
    {
        Configs = Directory.GetFiles(AutomationConfigsFolderPath, "*.json")
                           .Select(Path.GetFileNameWithoutExtension)
                           .SkipWhile(x => x is null)
                           .ToList()!;
    }

    ObservableCollection<Workflow> _automations = [];
    public ObservableCollection<Workflow> Workflows
    {
        get => _automations;
        set
        {
            if (Equals(value, _automations)) return;
            _automations = value;
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
    
    public ILogger<AutomationService> Logger { get; }
    public IRulesetService RulesetService { get; }
    public SettingsService SettingsService { get; }
    public IActionService ActionService { get; }
    public IWindowRuleService WindowRuleService { get; }
}
