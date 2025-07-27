using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    public ThemesSettingsPage()
    {

        InitializeComponent();
        DataContext = this;
        ViewModel.PluginMarketService.ObservableForProperty(x => x.Exception)
            .Subscribe(_ =>
            {
                if (ViewModel.PluginMarketService.Exception == null)
                {
                    return;
                }

                this.ShowErrorToast("无法刷新市场", ViewModel.PluginMarketService.Exception);
            });

    }

    private void ButtonLoadThemes_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.XamlThemeService.LoadThemeSource();
        ViewModel.XamlThemeService.LoadAllThemes();
    }

    [RelayCommand]
    private void OpenFolder(ThemeInfo info)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = System.IO.Path.GetFullPath(info.Path),
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void ShowErrors(ThemeInfo info)
    {
        OpenDrawer("ErrorInfoDrawer", dataContext: info.Error?.ToString());
    }

    private void ButtonOpenThemeFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = ClassIsland.Services.XamlThemeService.ThemesPath,
            UseShellExecute = true
        });
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
        var file = await topLevel.StorageProvider
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
            });
        var path = file?.TryGetLocalPath();
        if (path == null)
            return;
        try
        {
            await ViewModel.XamlThemeService.PackageThemeAsync(info.Manifest.Id, path);
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(path) ?? "",
                UseShellExecute = true
            });
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
}