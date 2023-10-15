using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Interfaces;
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