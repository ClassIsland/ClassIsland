using ClassIsland.Core.Abstractions.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AppearanceSettingsPage.xaml 的交互逻辑
/// </summary>
[Group("classisland.mainwindow")]
[SettingsPageInfo("appearance", "外观", "\ue51e", "\ue51d", SettingsPageCategory.Internal)]
public partial class AppearanceSettingsPage : SettingsPageBase
{
    public AppearanceSettingsViewModel ViewModel { get; } = IAppHost.GetService<AppearanceSettingsViewModel>();

    public AppearanceSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void ButtonUpdateWallpaper_OnClick(object sender, SelectionChangedEventArgs e)
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

    public override IReadOnlyList<string> GetSettingsResetTargetProperties() =>
    [
        nameof(Settings.Scale), nameof(Settings.BackgroundColor), nameof(Settings.IsCustomBackgroundColorEnabled),
        nameof(Settings.Opacity), nameof(Settings.RadiusX), nameof(Settings.MainWindowLineVerticalMargin),
        nameof(Settings.IsIslandSeperated), nameof(Settings.Theme), nameof(Settings.ColorSource),
        nameof(Settings.PrimaryColor), nameof(Settings.MainWindowFont), nameof(Settings.MainWindowFontWeight2),
        nameof(Settings.MainWindowSecondaryFontSize), nameof(Settings.MainWindowBodyFontSize),
        nameof(Settings.MainWindowEmphasizedFontSize), nameof(Settings.MainWindowLargeFontSize),
        nameof(Settings.CustomForegroundColor), nameof(Settings.IsCustomForegroundColorEnabled)
    ];
}
