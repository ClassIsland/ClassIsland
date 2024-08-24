using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace ClassIsland.ExamplePlugin.Views.SettingsPages;

[SettingsPageInfo("classisland.example-plugin.hello", "Hello world!")]
public partial class HelloSettingsPage : SettingsPageBase
{
    public HelloSettingsPage()
    {
        InitializeComponent();
    }
}