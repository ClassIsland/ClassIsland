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
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Shared.Helpers;

namespace ClassIsland.Core.Controls.Ruleset;

/// <summary>
/// RulesetControl.xaml 的交互逻辑
/// </summary>
public partial class RulesetControl : UserControl
{
    /// <summary>
    /// 添加规则命令。
    /// </summary>
    public static readonly ICommand AddRuleCommand = new RoutedUICommand();

    /// <summary>
    /// 删除规则命令。
    /// </summary>
    public static readonly ICommand RemoveRuleCommand = new RoutedUICommand();

    /// <summary>
    /// 添加规则组命令。
    /// </summary>
    public static readonly ICommand RemoveGroupCommand = new RoutedUICommand();

    /// <summary>
    /// 复制规则组命令。
    /// </summary>
    public static readonly ICommand DuplicateGroupCommand = new RoutedUICommand();


    public static readonly DependencyProperty RulesetProperty = DependencyProperty.Register(
        nameof(Ruleset), typeof(Models.Ruleset.Ruleset), typeof(RulesetControl), new PropertyMetadata(default(Models.Ruleset.Ruleset)));

    public Models.Ruleset.Ruleset Ruleset
    {
        get { return (Models.Ruleset.Ruleset)GetValue(RulesetProperty); }
        set { SetValue(RulesetProperty, value); }
    }

    /// <inheritdoc />
    public RulesetControl()
    {
        InitializeComponent();
    }

    private void ButtonAddRule_OnClick(object sender, RoutedEventArgs e)
    {
        Ruleset.Groups.Add(new RuleGroup());
    }

    private void CommandAddRule_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not RuleGroup group)
        {
            return;
        }
        group.Rules.Add(new Rule());
    }

    private void CommandRemoveRuleCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Rule rule)
        {
            return;
        }

        foreach (var group in Ruleset.Groups)
        {
            group.Rules.Remove(rule);
        }
    }

    private void CommandRemoveGroupCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not RuleGroup group)
        {
            return;
        }

        Ruleset.Groups.Remove(group);
    }

    private void CommandDuplicateGroup_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not RuleGroup group)
        {
            return;
        }

        Ruleset.Groups.Add(ConfigureFileHelper.CopyObject(group));
    }
}