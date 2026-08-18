using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Data;
using ReactiveUI;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// ThemesSettingsPage.xaml 的交互逻辑
/// </summary>
[FullWidthPage]
[SettingsPageInfo("classisland.themes", "主题", "\ue6af", "\ue6ae")]
public partial class ThemesSettingsPage : SettingsPageBase
{
    public ThemesSettingsViewModel ViewModel { get; } = IAppHost.GetService<ThemesSettingsViewModel>();

    private IDisposable? _pluginMarketExceptionSubscription;

    public ThemesSettingsPage()
    {

        InitializeComponent();
        DataContext = this;

    }

    private void ThemesSettingsPage_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _pluginMarketExceptionSubscription ??= ViewModel.PluginMarketService.ObservableForProperty(x => x.Exception)
            .Subscribe(_ =>
            {
                if (ViewModel.PluginMarketService.Exception == null)
                {
                    return;
                }

                this.ShowErrorToast("无法刷新市场", ViewModel.PluginMarketService.Exception);
            });
    }

    private void ThemesSettingsPage_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _pluginMarketExceptionSubscription?.Dispose();
        _pluginMarketExceptionSubscription = null;
    }

    private void ButtonLoadThemes_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.XamlThemeService.LoadThemeSource();
        ViewModel.XamlThemeService.LoadAllThemes();
        ViewModel.UpdateMergedThemes();
    }

    [RelayCommand]
    private void OpenFolder(ThemeInfo info)
    {
        PlatformServices.LauncherService.LaunchPath(info.Path);
    }

    [RelayCommand]
    private void ShowErrors(ThemeInfo info)
    {
        OpenDrawer("ErrorInfoDrawer", dataContext: info.Error?.ToString());
    }

    private void ButtonOpenThemeFolder_OnClick(object sender, RoutedEventArgs e)
    {
        PlatformServices.LauncherService.LaunchPath(ClassIsland.Services.XamlThemeService.ThemesPath);
    }

    private void ListBoxCategory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.UpdateMergedThemes();
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        RequestRestart();
    }

    [RelayCommand]
    private void InstallTheme(string id)
    {
        ViewModel.XamlThemeService.RequestDownloadTheme(id);
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.PluginMarketService.RefreshPluginSourceAsync();
        ViewModel.UpdateMergedThemes();
    }
    

    [RelayCommand]
    private void UninstallTheme(ThemeInfo info)
    {
        info.IsUninstalling = true;
        RequestRestart();
    }

    [RelayCommand]
    private void UndoUninstallTheme(ThemeInfo info)
    {
        info.IsUninstalling = false;
    }

    [RelayCommand]
    private async Task PackageThemeCommand(ThemeInfo? info)
    {
        if (info == null)
            return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }
        PopupHelper.DisableAllPopups();
        var file = await PlatformServices.FilePickerService
            .SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                SuggestedFileName = info.Manifest.Id + ".zip",
                Title = "打包主题",
                FileTypeChoices = [
                    new FilePickerFileType("ClassIsland 主题包")
                    {
                        Patterns = ["*.zip"]
                    }
                ]
            }, topLevel);
        PopupHelper.RestoreAllPopups();
        if (file == null)
            return;
        try
        {
            using var storageFile = await PlatformServices.FilePickerService.GetFileAsync(file, topLevel)
                                    ?? throw new FileNotFoundException("无法打开所选主题包文件。", file);
            await using var outputStream = await storageFile.OpenWriteAsync();
            if (outputStream.CanSeek)
            {
                outputStream.SetLength(0);
                outputStream.Position = 0;
            }
            await ViewModel.XamlThemeService.PackageThemeAsync(info.Manifest.Id, outputStream);
            var launchPath = PlatformServices.FilePickerService.IsBookmark(file)
                ? file
                : Path.GetDirectoryName(file) ?? file;
            await PlatformServices.LauncherService.LaunchPath(launchPath);
        }
        catch (Exception ex)
        {
            this.ShowErrorToast($"无法打包主题 {info.Manifest.Id}", ex);
        }
    }

    private void ButtonAgreeAgreement_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.IsThemeWarningVisible = false;
    }

    private void ToggleButtonIsThemeEnabled_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch { DataContext: KeyValuePair<string, ThemeInfo> kvp } switcher)
        {
            return;
        }

        if (switcher.IsChecked.Equals(true) && !ViewModel.XamlThemeService.EnabledThemes.Contains(kvp.Key))
        {
            ViewModel.XamlThemeService.EnabledThemes.Add(kvp.Key);
        }

        if (switcher.IsChecked.Equals(true))
            return;
        if (ViewModel.XamlThemeService.EnabledThemes.Count <= 1)
        {
            this.ShowWarningToast("您必须启用至少一个主题，以保证主界面有显示样式可用。");
            Dispatcher.UIThread.InvokeAsync(() => switcher.IsChecked = true);
            return;
        }
        ViewModel.XamlThemeService.EnabledThemes.Remove(kvp.Key);
    }

    private void ButtonOpenThemeLoadOrderDrawer_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDrawer("ThemeSortingDrawer");
    }
}
