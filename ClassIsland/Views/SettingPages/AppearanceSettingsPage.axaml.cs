using ClassIsland.Core.Abstractions.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Attributes;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AppearanceSettingsPage.xaml 的交互逻辑
/// </summary>
[Group("classisland.mainwindow")]
[SettingsPageInfo("appearance", "外观", "\ue51e", "\ue51d", SettingsPageCategory.Internal)]
public partial class AppearanceSettingsPage : SettingsPageBase
{
    public static readonly FuncValueConverter<double, string?> BackgroundImageOpacityDescriptionConverter = new(
        opacity => opacity > 0.4 ? "过高的背景不透明度可能会导致界面内容难以阅读。" : null);

    public AppearanceSettingsViewModel ViewModel { get; } = IAppHost.GetService<AppearanceSettingsViewModel>();

    public AppearanceSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void SelectBackgroundImage_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.IsSelectingBackgroundImage)
        {
            return;
        }

        ViewModel.BackgroundImageError = "";
        var root = TopLevel.GetTopLevel(this);
        if (root == null)
        {
            ViewModel.BackgroundImageError = "暂时无法选择图片，请稍后重试。";
            return;
        }

        ViewModel.IsSelectingBackgroundImage = true;
        var settings = ViewModel.SettingsService.Settings;
        var previousSource = settings.AndroidBackgroundImageSource;
        try
        {
            var files = await PlatformServices.FilePickerService.OpenBlobFilesPickerAsync(new BlobFilePickerOpenOptions
            {
                Title = "选择背景图片",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            }, root);
            if (files.Count == 0)
            {
                return;
            }

            var source = files[0];
            await Task.Run(() =>
            {
                using var stream = AssetLoader.Open(source);
                using var bitmap = new Bitmap(stream);
            });
            if (ViewModel.SettingsService.Settings == settings &&
                settings.AndroidBackgroundImageSource == previousSource)
            {
                settings.AndroidBackgroundImageSource = source.AbsoluteUri;
            }
        }
        catch (Exception)
        {
            ViewModel.BackgroundImageError = "无法使用这张图片，请重新选择其他图片。";
        }
        finally
        {
            ViewModel.IsSelectingBackgroundImage = false;
        }
    }

    private void RemoveBackgroundImage_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.AndroidBackgroundImageSource = "";
        ViewModel.BackgroundImageError = "";
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
}
