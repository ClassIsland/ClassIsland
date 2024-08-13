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

namespace ClassIsland.Core.Controls.Ruleset;

/// <summary>
/// RulesetControl.xaml 的交互逻辑
/// </summary>
public partial class RulesetControl : UserControl
{
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
        Ruleset.Rules.Add(new Rule());
    }
}