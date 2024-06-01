using System;
using System.Windows.Controls;

using ClassIsland.Shared.Interfaces;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// ClassNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class ClassNotificationAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public ClassNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } =
        new AttachedSettingsControlHelper<ClassNotificationAttachedSettings>(
            new Guid("08F0D9C3-C770-4093-A3D0-02F3D90C24BC"), new ClassNotificationAttachedSettings());

    public ClassNotificationAttachedSettings Settings =>
        ((AttachedSettingsControlHelper<ClassNotificationAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings ?? new ClassNotificationAttachedSettings();
}