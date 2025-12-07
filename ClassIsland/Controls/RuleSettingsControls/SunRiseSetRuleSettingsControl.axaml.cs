using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Rules;

namespace ClassIsland.Controls.RuleSettingsControls;

public partial class SunRiseSetRuleSettingsControl : RuleSettingsControlBase<SunRiseSetRuleSettings>
{
    public SunRiseSetRuleSettingsControl()
    {
        InitializeComponent();
    }
}
