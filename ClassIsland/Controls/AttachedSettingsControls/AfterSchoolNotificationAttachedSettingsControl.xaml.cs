using System;
using System.Windows.Controls;

using ClassIsland.Core.Interfaces;
using ClassIsland.Models.AttachedSettings;

namespace ClassIsland.Controls.AttachedSettingsControls;

/// <summary>
/// AfterSchoolNotificationAttachedSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class AfterSchoolNotificationAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public AfterSchoolNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } = 
        new AttachedSettingsControlHelper<AfterSchoolNotificationAttachedSettings>(
        new Guid("8FBC3A26-6D20-44DD-B895-B9411E3DDC51"), new AfterSchoolNotificationAttachedSettings());

    public AfterSchoolNotificationAttachedSettings Settings =>
        ((AttachedSettingsControlHelper<AfterSchoolNotificationAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings ?? new AfterSchoolNotificationAttachedSettings();
}