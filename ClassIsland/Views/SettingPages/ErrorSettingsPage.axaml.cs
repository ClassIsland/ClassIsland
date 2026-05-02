using System.Web;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

[HidePageTitle]
[SettingsPageInfo("_error", "出错啦！", category: SettingsPageCategory.About, hideDefault: true)]
public partial class ErrorSettingsPage : SettingsPageBase
{
    public ErrorSettingsViewModel ViewModel { get; } = IAppHost.GetService<ErrorSettingsViewModel>();

    public ErrorSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsError = HttpUtility.ParseQueryString(NavigationUri?.Query ?? "")["error"] == "true";
    }
}