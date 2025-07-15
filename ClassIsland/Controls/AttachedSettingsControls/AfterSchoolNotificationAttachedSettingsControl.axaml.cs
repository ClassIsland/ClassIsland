using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// AfterSchoolNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.ClassPlan | AttachedSettingsTargets.TimeLayout)]
[AttachedSettingsControlInfo("8FBC3A26-6D20-44DD-B895-B9411E3DDC51", "放学提醒设置", "\ued34")]
public partial class AfterSchoolNotificationAttachedSettingsControl : AttachedSettingsControlBase<AfterSchoolNotificationAttachedSettings>
{
    public AfterSchoolNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }
}

