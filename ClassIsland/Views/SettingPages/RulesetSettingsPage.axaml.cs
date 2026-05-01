using System;
using System.Windows;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("rulesets", "共享规则集", "\uEF5B", "\uEF5C", SettingsPageCategory.Internal)]
public partial class RulesetSettingsPage : SettingsPageBase
{
    public RulesetSettingsViewModel ViewModel { get; } = IAppHost.GetService<RulesetSettingsViewModel>();

    public RulesetSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
    }

    [RelayCommand]
    private void AddNamedRuleset()
    {
        var ruleset = ViewModel.NamedRulesetService.AddNamedRuleset("新建规则集");
        ViewModel.SelectedRuleset = ruleset;
        ViewModel.IsPanelOpened = true;
    }

    [RelayCommand]
    private void DeleteNamedRuleset(NamedRuleset ruleset)
    {
        ViewModel.NamedRulesetService.RemoveNamedRuleset(ruleset);
        if (ViewModel.SelectedRuleset == ruleset)
        {
            ViewModel.SelectedRuleset = null;
            ViewModel.IsPanelOpened = false;
        }
    }

    [RelayCommand]
    private void DuplicateNamedRuleset(NamedRuleset ruleset)
    {
        var copy = ConfigureFileHelper.CopyObject(ruleset);
        copy.Id = Guid.NewGuid();
        copy.Name = ruleset.Name + " - 副本";
        ViewModel.NamedRulesetService.NamedRulesets.Add(copy);
        ViewModel.NamedRulesetService.SaveConfig();
        ViewModel.SelectedRuleset = copy;
    }
}
