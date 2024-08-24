using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("test-settings-page", "测试页面", SettingsPageCategory.Debug)]
public partial class TestSettingsPage : SettingsPageBase
{
    public TestSettingsPage()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        NavigationService!.Navigate(new Page());
    }
}