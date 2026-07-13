using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Rules;

namespace ClassIsland.Controls.RuleSettingsControls;

[ContributorInfo("@baiyao")]
public partial class SunRiseSetRuleSettingsControl : RuleSettingsControlBase<SunRiseSetRuleSettings>
{
    public SunRiseSetRuleSettingsControl()
    {
        InitializeComponent();
    }
}
