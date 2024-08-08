using System;
using System.Windows.Controls;

using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// AfterSchoolNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.ClassPlan | AttachedSettingsTargets.TimeLayout)]
[AttachedSettingsControlInfo("8FBC3A26-6D20-44DD-B895-B9411E3DDC51", "放学提醒设置", PackIconKind.RunFast)]
public partial class AfterSchoolNotificationAttachedSettingsControl
{
    public AfterSchoolNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }
}