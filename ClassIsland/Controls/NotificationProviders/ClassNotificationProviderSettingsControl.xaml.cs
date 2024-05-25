using System.Windows.Controls;

using ClassIsland.Models.NotificationProviderSettings;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// ClassNotificationProviderSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class ClassNotificationProviderSettingsControl : UserControl
{
    public ClassNotificationSettings Settings
    {
        get;
        set;
    }

    public ClassNotificationProviderSettingsControl(ClassNotificationSettings settings)
    {
        Settings = settings;
        InitializeComponent();
    }
}