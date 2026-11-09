using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Icons;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

[Group("classisland.general")]
[SettingsPageInfo("interface", "界面", FluentIcons.WindowRegular, FluentIcons.WindowFilled, SettingsPageCategory.Internal)]
public partial class InterfaceSettingsPage : SettingsPageBase
{
    public static readonly FuncValueConverter<double, string?> BackgroundImageOpacityDescriptionConverter = new(
        opacity => opacity > 0.4 ? "过高的背景不透明度可能会导致界面内容难以阅读。" : null);

    public InterfaceSettingsViewModel ViewModel { get; } = IAppHost.GetService<InterfaceSettingsViewModel>();

    public InterfaceSettingsPage()
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
}
