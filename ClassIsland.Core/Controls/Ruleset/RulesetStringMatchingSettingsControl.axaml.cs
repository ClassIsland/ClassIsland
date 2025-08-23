using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Models.Ruleset;

namespace ClassIsland.Core.Controls.Ruleset;

/// <summary>
/// RulesetStringMatchingSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class RulesetStringMatchingSettingsControl : RuleSettingsControlBase<StringMatchingSettings>
{
    /// <inheritdoc />
    public RulesetStringMatchingSettingsControl()
    {
        InitializeComponent();
    }
}
