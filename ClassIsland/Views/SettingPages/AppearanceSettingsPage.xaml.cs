using ClassIsland.Core.Abstractions.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using ClassIsland.Core.Attributes;
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

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
    }
}