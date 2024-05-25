using System.Windows.Controls;

using ClassIsland.Models.NotificationProviderSettings;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// AfterSchoolNotificationProviderSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class AfterSchoolNotificationProviderSettingsControl : UserControl
{
    public AfterSchoolNotificationProviderSettings Settings { get; }

    public AfterSchoolNotificationProviderSettingsControl(AfterSchoolNotificationProviderSettings settings)
    {
        Settings = settings;
        InitializeComponent();
    }
}