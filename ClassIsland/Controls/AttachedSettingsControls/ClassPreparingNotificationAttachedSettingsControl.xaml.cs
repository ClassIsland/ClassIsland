﻿using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// ClassNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.ClassPlan | AttachedSettingsTargets.TimeLayout |
                       AttachedSettingsTargets.Lesson | AttachedSettingsTargets.Subject |
                       AttachedSettingsTargets.TimePoint)]
[AttachedSettingsControlInfo("08F0D9C3-C770-4093-A3D0-02F3D90C24BC", "准备上课提醒设置", PackIconKind.BellNotificationOutline)]
public partial class ClassPreparingNotificationAttachedSettingsControl
{
    public ClassPreparingNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }
}