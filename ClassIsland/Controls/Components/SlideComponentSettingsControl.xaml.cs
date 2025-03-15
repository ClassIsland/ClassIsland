using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Core.Models.Ruleset;

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