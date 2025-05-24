using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Models.NotificationProviderSettings;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// AfterSchoolNotificationProviderSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class AfterSchoolNotificationProviderSettingsControl
{
    public AfterSchoolNotificationProviderSettingsControl()
    {
        InitializeComponent();
    }

    private void ButtonShowAttachedSettingsInfo_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsPageBase.OpenDrawerCommand.Execute(new RootAttachedSettingsDependencyControl(IAttachedSettingsHostService.RegisteredControls.First(x => x.Guid == new Guid("8FBC3A26-6D20-44DD-B895-B9411E3DDC51"))));
    }
}