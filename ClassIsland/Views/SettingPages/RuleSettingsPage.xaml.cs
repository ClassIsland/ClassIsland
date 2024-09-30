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
using ClassIsland.Core.Models;

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

    public static readonly ICommand RemoveCommand = new RoutedUICommand();
    public static readonly ICommand DuplicateCommand = new RoutedUICommand();
    public static readonly ICommand DebugInvokeActionCommand = new RoutedUICommand();
    public static readonly ICommand DebugInvokeBackActionCommand = new RoutedUICommand();

    public ObservableCollection<RuleActionPair> RuleActionPairs
    {
        get => SettingsService.Settings.RuleActionPairs;
        set => SettingsService.Settings.RuleActionPairs = value;
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        RuleActionPairs.Add(new());
    }

    private void CommandRemoveCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not RuleActionPair ruleActionPair) return;

        RuleActionPairs.Remove(ruleActionPair);
    }

    private void CommandDuplicate_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not RuleActionPair ruleActionPair) return;

        RuleActionPairs.Add(ConfigureFileHelper.CopyObject(ruleActionPair));
    }

    private void CommandDebugInvokeAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not RuleActionPair ruleActionPair) return;

        App.GetService<IActionService>().InvokeActionList(ruleActionPair.ActionList);
    }

    private void CommandDebugInvokeBackAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not RuleActionPair ruleActionPair) return;

        App.GetService<IActionService>().InvokeBackActionList(ruleActionPair.ActionList);
    }
}