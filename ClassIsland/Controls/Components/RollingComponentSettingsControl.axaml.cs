using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls.Ruleset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Services;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Controls.Components;

/// <summary>
/// RollingComponentSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class RollingComponentSettingsControl : ComponentBase<RollingComponentSettings>
{
    public SettingsService SettingsService { get; }

    public RollingComponentSettingsControl(SettingsService settingsService)
    {
        SettingsService = settingsService;
        InitializeComponent();
    }

    private void OpenDrawer(string key)
    {
        if (this.FindResource(key) is not Control drawer)
        {
            return;
        }

        drawer.DataContext = this;
        SettingsPageBase.OpenDrawerCommand.Execute(drawer);
    }

    private void ButtonOpenPauseRuleset_OnClick(object sender, RoutedEventArgs e)
    {
        if (this.FindResource("RulesetControl") is RulesetControl rulesetControl)
            rulesetControl.Ruleset = Settings.PauseRule;
        OpenDrawer("RulesetControl");
    }

    private void ButtonOpenStopRuleset_OnClick(object sender, RoutedEventArgs e)
    {
        if (this.FindResource("RulesetControl") is RulesetControl rulesetControl)
            rulesetControl.Ruleset = Settings.StopRule;
        OpenDrawer("RulesetControl");
    }

    private void ButtonCloseWarningTip_OnClick(InfoBar infoBar, EventArgs args)
    {
        SettingsService.Settings.IsRollingComponentWarningVisible = false;
    }
}
