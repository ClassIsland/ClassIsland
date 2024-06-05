using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("test-settings-page", "测试页面")]
public partial class TestSettingsPage : SettingsPageBase
{
    public TestSettingsPage()
    {
        InitializeComponent();
    }
}