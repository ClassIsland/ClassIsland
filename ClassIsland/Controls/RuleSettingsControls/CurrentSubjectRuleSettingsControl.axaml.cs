using System;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Models.Rules;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
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
