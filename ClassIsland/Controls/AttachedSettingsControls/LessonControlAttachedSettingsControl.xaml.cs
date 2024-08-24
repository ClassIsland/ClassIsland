using System;
using System.Windows.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// LessonControlAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.ClassPlan | AttachedSettingsTargets.TimeLayout |
                       AttachedSettingsTargets.Lesson | AttachedSettingsTargets.Subject |
                       AttachedSettingsTargets.TimePoint)]
[AttachedSettingsControlInfo("58e5b69a-764a-472b-bcf7-003b6a8c7fdf", "课程显示设置", PackIconKind.BookSearchOutline)]
public partial class LessonControlAttachedSettingsControl
{
    public LessonControlAttachedSettingsControl()
    {
        InitializeComponent();
    }
}