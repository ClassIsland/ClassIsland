using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
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
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.SettingPages;

[Group("classisland.general")]
[SettingsPageInfo("interface", "界面", FluentIcons.WindowRegular, FluentIcons.WindowFilled, SettingsPageCategory.Internal)]
public partial class InterfaceSettingsPage : SettingsPageBase
{
    public static readonly FuncValueConverter<double, string?> BackgroundImageOpacityDescriptionConverter = new(
        opacity => opacity > 0.4 ? "过高的背景不透明度可能会导致界面内容难以阅读。" : null);

    public static readonly FuncValueConverter<int, bool> IsCustomSource = new(source => source == 0);
    public static readonly FuncValueConverter<int, bool> IsWallpaperSource = new(source => source == 1);
    public static readonly FuncMultiValueConverter<object, bool> ShowWallpaperControls = new(values =>
        values.ToArray() is [1, true]);
    public static readonly FuncMultiValueConverter<object, bool> ShowAccentInterval = new(values =>
        values.ToArray() is [1, true, true, _] or [3, _, _, true]);
    public static readonly FuncMultiValueConverter<object, bool> ShowAccentControls = new(values =>
        values.ToArray() is [1, true, _] or [3, _, true]);
    public static readonly FuncMultiValueConverter<object, bool> ShowUnsupportedAccent = new(values =>
        values.ToArray() is [1, false, _] or [3, _, false]);
    public static readonly IValueConverter AccentIntervalConverter = new RefreshIntervalConverter();

    public InterfaceSettingsViewModel ViewModel { get; } = IAppHost.GetService<InterfaceSettingsViewModel>();
    private bool _isAccentDrawerActive;

    private IViewHost? AvailableAccentDrawerHost
    {
        get
        {
            if (_isAccentDrawerActive)
                return null;

            var owner = Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<ViewBase>(this);
            return owner?.AssociatedViewHost is { IsDrawerOpen: false } host ? host : null;
        }
    }

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

    private void AccentSource_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ViewModel.AccentError = null;
        if (sender is ComboBox combo && AccentSourceSettings is { } expander)
        {
            expander.SetCurrentValue(FASettingsExpander.IsExpandedProperty, combo.SelectedIndex switch
            {
                0 => true,
                1 => ViewModel.AccentPicker.SupportsWallpaper,
                3 => ViewModel.AccentPicker.SupportsScreen,
                _ => false
            });
        }
    }

    private async void RefreshAccent_OnClick(object? sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAccentAsync();

    private async void PreviewAccent_OnClick(object? sender, RoutedEventArgs e)
    {
        if (AvailableAccentDrawerHost is not { } host)
            return;

        using var preview = new AccentPreviewView(ViewModel);
        await ShowAccentDrawerAsync(host, preview);
    }

    private async void BrowseWallpaperWindows_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.ScreenshotService is not { } screenshots || AvailableAccentDrawerHost is not { } host)
            return;
        var settings = ViewModel.SettingsService.Settings;
        using var browser = new WallpaperWindowPicker(screenshots, settings.WallpaperClassName);
        await ShowAccentDrawerAsync(host, browser);
        if (browser.Result is not string className || settings != ViewModel.SettingsService.Settings ||
            settings.ColorSource != 1 || settings.IsFallbackModeEnabled)
            return;

        settings.WallpaperClassName = className;
        await ViewModel.RefreshAccentAsync();
    }

    private async Task ShowAccentDrawerAsync(IViewHost host, ViewBase view)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnClosed(object? sender, RoutedEventArgs e) => closed.TrySetResult();

        _isAccentDrawerActive = true;
        view.Closed += OnClosed;
        try
        {
            await host.ShowDrawer(view);
            if (ReferenceEquals(host.CurrentDrawerView, view))
                await closed.Task;
        }
        finally
        {
            view.Closed -= OnClosed;
            _isAccentDrawerActive = false;
        }
    }

    private sealed class RefreshIntervalConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is int seconds and > 0 ? (decimal)Math.Min(seconds, 36000) : 60m;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is decimal seconds ? (int)Math.Clamp(decimal.Round(seconds), 1, 36000) : BindingOperations.DoNothing;
    }
}
