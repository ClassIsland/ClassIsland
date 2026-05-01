using System.ComponentModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Services;
using ClassIsland.Views.SettingPages;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class RulesetSettingsViewModel : ObservableRecipient
{
    public INamedRulesetService NamedRulesetService { get; }
    public IRulesetService RulesetService { get; }
    public SettingsService SettingsService { get; }
    public ILogger<RulesetSettingsPage> Logger { get; }

    [ObservableProperty]
    private bool _isPanelOpened = false;

    [ObservableProperty]
    private NamedRuleset? _selectedRuleset;

    partial void OnIsPanelOpenedChanged(bool value)
    {
        if (!value)
            SelectedRuleset = null;
    }

    public RulesetSettingsViewModel(
        INamedRulesetService namedRulesetService,
        IRulesetService rulesetService,
        SettingsService settingsService,
        ILogger<RulesetSettingsPage> logger)
    {
        NamedRulesetService = namedRulesetService;
        RulesetService = rulesetService;
        SettingsService = settingsService;
        Logger = logger;

        NamedRulesetService.NamedRulesetsUpdated += (_, _) => OnPropertyChanged(nameof(NamedRulesetService));
        NamedRulesetService.NamedRulesets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(NamedRulesetService));
        if (SelectedRuleset != null)
        {
            SelectedRuleset.PropertyChanged += OnSelectedRulesetPropertyChanged;
        }
    }

    partial void OnSelectedRulesetChanged(NamedRuleset? value)
    {
        if (value != null)
        {
            value.PropertyChanged += OnSelectedRulesetPropertyChanged;
        }
    }

    private void OnSelectedRulesetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NamedRuleset.Name) or nameof(NamedRuleset.Description)
            or nameof(NamedRuleset.Ruleset))
        {
            NamedRulesetService.SaveConfig();
        }
    }
}
