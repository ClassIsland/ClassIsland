using ClassIsland.Core.Abstractions.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using Avalonia.Interactivity;
using ClassIsland.Core.Attributes;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AppearanceSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("appearance", "外观", "\ue51e", "\ue51d", SettingsPageCategory.Internal)]
public partial class AppearanceSettingsPage : SettingsPageBase
{
    public AppearanceSettingsViewModel ViewModel { get; } = IAppHost.GetService<AppearanceSettingsViewModel>();

    public AppearanceSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void ButtonUpdateWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        // if (SettingsService.Settings.ColorSource is 1 or 3)
        //     await WallpaperPickingService.GetWallpaperAsync();
    }

    private void ButtonPreviewWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        // var w = App.GetService<WallpaperPreviewWindow>();
        // w.Owner = Window.GetWindow(this);
        // w.ShowDialog();
    }

    private async void ButtonBrowseWindows_OnClick(object sender, RoutedEventArgs e)
    {
        // var w = new WindowsPicker(SettingsService.Settings.WallpaperClassName)
        // {
        //     Owner = Window.GetWindow(this),
        // };
        // var r = w.ShowDialog();
        // SettingsService.Settings.WallpaperClassName = w.SelectedResult ?? "";
        // if (r == true)
        // {
        //     await WallpaperPickingService.GetWallpaperAsync();
        // }
        // GC.Collect();
    }
}
