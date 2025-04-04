using System.Windows;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls.Ruleset;

namespace ClassIsland.Controls.Components;

/// <summary>
/// SlideComponentSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class SlideComponentSettingsControl
{
    private void OpenDrawer(string key)
    {
        if (FindResource(key) is not FrameworkElement drawer)
        {
            return;
        }

        drawer.DataContext = this;
        SettingsPageBase.OpenDrawerCommand.Execute(drawer);
    }

    public SlideComponentSettingsControl()
    {
        InitializeComponent();
    }

    private void ButtonOpenPauseRuleset_OnClick(object sender, RoutedEventArgs e)
    {
        if (FindResource("RulesetControl") is RulesetControl rulesetControl) 
            rulesetControl.Ruleset = Settings.PauseRule;
        OpenDrawer("RulesetControl");
    }

    private void ButtonOpenStopRuleset_OnClick(object sender, RoutedEventArgs e)
    {
        if (FindResource("RulesetControl") is RulesetControl rulesetControl)
            rulesetControl.Ruleset = Settings.StopRule;
        OpenDrawer("RulesetControl");
    }
}