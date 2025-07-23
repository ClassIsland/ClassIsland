using System;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.SettingsWindow;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;

namespace ClassIsland.Views.SettingPages;

[HidePageTitle]
[SettingsPageInfo("_error", "出错啦！", category:SettingsPageCategory.About, hideDefault:true)]
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