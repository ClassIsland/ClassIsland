#if false
using System.Windows.Controls;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// AfterSchoolNotificationProvider.xaml 的交互逻辑
/// </summary>
public partial class AfterSchoolNotificationProviderControl : UserControl
{
    public string Message { get; }

    public object? ShowContent { get; }

    public AfterSchoolNotificationProviderControl(string message, string key)
    {
        Message = message;
        InitializeComponent();
        ShowContent = FindResource(key);
    }
}
#endif
