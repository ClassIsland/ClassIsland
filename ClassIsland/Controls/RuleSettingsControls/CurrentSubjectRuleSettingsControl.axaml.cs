using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Models.Rules;
using ClassIsland.ViewModels;

namespace ClassIsland.Controls.RuleSettingsControls;

/// <summary>
/// CurrentSubjectRuleSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class CurrentSubjectRuleSettingsControl : RuleSettingsControlBase<CurrentSubjectRuleSettings>
{
    public ProfileSettingsViewModel ProfileSettingsViewModel { get; }

    public CurrentSubjectRuleSettingsControl(ProfileSettingsViewModel vm)
    {
        ProfileSettingsViewModel = vm;
        InitializeComponent();
    }
}
