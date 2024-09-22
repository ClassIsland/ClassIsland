using System.Windows;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Core.Models.Action;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Shared.Helpers;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// RuleSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("rules", "规则", PackIconKind.TagMultipleOutline, PackIconKind.TagMultiple, SettingsPageCategory.Internal)]
public partial class RuleSettingsPage
{
    public RulesetSettingsViewModel ViewModel { get; } = new();

    public IRulesetService RulesetService { get; }
    public SettingsService SettingsService { get; }
    public ILogger<RuleSettingsPage> Logger { get; }

    public RuleSettingsPage(IRulesetService rulesetService, SettingsService settingsService, ILogger<RuleSettingsPage> logger)
    {
        RulesetService = rulesetService;
        SettingsService = settingsService;
        Logger = logger;
        DataContext = this;
        InitializeComponent();
    }

    public static readonly ICommand AddRuleCommand = new RoutedUICommand();
    public static readonly ICommand RemoveRuleCommand = new RoutedUICommand();
    public static readonly ICommand RemoveRulesetCommand = new RoutedUICommand();
    public static readonly ICommand DuplicateRulesetCommand = new RoutedUICommand();
    public static readonly ICommand DebugInvokeActionCommand = new RoutedUICommand();
    public static readonly ICommand DebugInvokeBackActionCommand = new RoutedUICommand();

    public ObservableCollection<Ruleset> Rulesets
    {
        get => SettingsService.Settings.Rules;
        set => SettingsService.Settings.Rules = value;
    }

    private void ButtonAddRule_OnClick(object sender, RoutedEventArgs e)
    {
        Rulesets.Add(new() { Groups = [new() { Rules = new() { new() } }] });
    }

    private void CommandAddRule_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Ruleset ruleset) return;

        ruleset.Groups[0].Rules.Add(new Rule());
    }

    private void CommandRemoveRuleCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Rule rule) return;

        foreach (var ruleset in Rulesets)
        {
            var isRemoved = ruleset.Groups[0].Rules.Remove(rule);
            if (isRemoved) return;
        }
    }

    private void CommandRemoveRulesetCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Ruleset ruleset) return;

        Rulesets.Remove(ruleset);
    }

    private void CommandDuplicateRuleset_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Ruleset ruleset) return;

        Rulesets.Add(ConfigureFileHelper.CopyObject(ruleset));
    }

    private void CommandDebugInvokeAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Ruleset ruleset) return;

        App.GetService<IActionService>().InvokeAction([.. ruleset.Actions]);
    }

    private void CommandDebugInvokeBackAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Ruleset ruleset) return;

        App.GetService<IActionService>().InvokeBackAction([.. ruleset.Actions]);
    }
}