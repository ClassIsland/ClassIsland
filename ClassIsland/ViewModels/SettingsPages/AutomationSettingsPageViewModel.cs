using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using ClassIsland.Views.SettingPages;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Workflow = ClassIsland.Core.Models.Automation.Workflow;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class AutomationSettingsViewModel : ObservableRecipient
{
    public IRulesetService RulesetService { get; }
    public SettingsService SettingsService { get; }
    public IAutomationService AutomationService { get; }
    public IActionService ActionService { get; }
    public ILogger<AutomationSettingsPage> Logger { get; }

    public AutomationSettingsViewModel(
        IRulesetService rulesetService,
        SettingsService settingsService,
        ILogger<AutomationSettingsPage> logger,
        IAutomationService automationService,
        IActionService actionService)
    {
        RulesetService = rulesetService;
        SettingsService = settingsService;
        Logger = logger;
        AutomationService = automationService;
        ActionService = actionService;
    }

    [ObservableProperty]
    bool _isPanelOpened = false;

    partial void OnIsPanelOpenedChanged(bool value)
    {
        if (!value)
            SelectedWorkflow = null;
    }

    [ObservableProperty]
    Workflow? _selectedWorkflow;
}