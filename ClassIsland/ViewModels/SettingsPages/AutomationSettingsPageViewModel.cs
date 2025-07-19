using System.Reactive;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models;
using ClassIsland.Services;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Action;
using ClassIsland.Views.SettingPages;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class AutomationSettingsViewModel
    : ObservableRecipient
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
    

    [ObservableProperty]
    Workflow? _selectedAutomation;


    [ObservableProperty]
    string _createProfileName = "";
}