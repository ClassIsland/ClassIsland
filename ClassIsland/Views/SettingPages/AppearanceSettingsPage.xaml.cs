using ClassIsland.Core.Abstractions.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;
using ClassIsland.Core.Enums.SettingsWindow;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AppearanceSettingsPage.xaml 的交互逻辑
/// </summary>

[SettingsPageInfo("appearance", "外观", PackIconKind.ThemeOutline, PackIconKind.Theme, SettingsPageCategory.Internal)]
public partial class AppearanceSettingsPage : SettingsPageBase
{
    public AppearanceSettingsViewModel ViewModel { get; } = new();

    public SettingsService SettingsService { get; }

    public WallpaperPickingService WallpaperPickingService { get; }

    public AppearanceSettingsPage(SettingsService settingsService, WallpaperPickingService wallpaperPickingService)
    {
        SettingsService = settingsService;
        WallpaperPickingService = wallpaperPickingService;
        InitializeComponent();
        DataContext = this;
    }

    private async void ButtonUpdateWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        if (SettingsService.Settings.ColorSource is 1 or 3)
            await WallpaperPickingService.GetWallpaperAsync();
    }

    private void ButtonPreviewWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        var w = App.GetService<WallpaperPreviewWindow>();
        w.Owner = Window.GetWindow(this);
        w.ShowDialog();
    }

    private async void ButtonBrowseWindows_OnClick(object sender, RoutedEventArgs e)
    {
        var w = new WindowsPicker(SettingsService.Settings.WallpaperClassName)
        {
            Owner = Window.GetWindow(this),
        };
        var r = w.ShowDialog();
        SettingsService.Settings.WallpaperClassName = w.SelectedResult ?? "";
        if (r == true)
        {
            await WallpaperPickingService.GetWallpaperAsync();
        }
        GC.Collect();
    }
}