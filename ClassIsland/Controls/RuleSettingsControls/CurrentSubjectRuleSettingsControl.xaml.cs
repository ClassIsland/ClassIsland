using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Controls.RuleSettingsControls;

/// <summary>
/// CurrentSubjectRuleSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class CurrentSubjectRuleSettingsControl
{
    public IProfileService ProfileService { get; }

    public CurrentSubjectRuleSettingsControl(IProfileService profileService)
    {
        ProfileService = profileService;
        InitializeComponent();
    }
}