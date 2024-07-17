using System;
using System.Windows.Controls;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// LessonControlAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class LessonControlAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public LessonControlAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } =
        new AttachedSettingsControlHelper<LessonControlAttachedSettings>(
            new Guid("58e5b69a-764a-472b-bcf7-003b6a8c7fdf"), new LessonControlAttachedSettings());

    public LessonControlAttachedSettings Settings =>
        ((AttachedSettingsControlHelper<LessonControlAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings ?? new LessonControlAttachedSettings();
}