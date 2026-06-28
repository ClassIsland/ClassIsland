using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

[Group("classisland.general")]
[SettingsPageInfo("tray-menu", "托盘菜单", "\uedbf", "\uedbf", SettingsPageCategory.Internal)]
public partial class TrayMenuSettingsPage : SettingsPageBase
{
    public TrayMenuSettingsViewModel ViewModel { get; } = IAppHost.GetService<TrayMenuSettingsViewModel>();

    public TrayMenuSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }
}
