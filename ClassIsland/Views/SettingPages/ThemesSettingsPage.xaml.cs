using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// ThemesSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("classisland.themes", "主题", PackIconKind.FileCodeOutline, PackIconKind.FileCode)]
public partial class ThemesSettingsPage
{
    public IXamlThemeService XamlThemeService { get; }
    public IPluginMarketService PluginMarketService { get; }
    public SettingsService SettingsService { get; }

    public ThemesSettingsViewModel ViewModel { get; } = new();

    public ThemesSettingsPage(IXamlThemeService xamlThemeService, IPluginMarketService pluginMarketService, SettingsService settingsService)
    {
        XamlThemeService = xamlThemeService;
        PluginMarketService = pluginMarketService;
        SettingsService = settingsService;
        InitializeComponent();
        DataContext = this;

        if (FindResource("DataProxy") is BindingProxy proxy)
        {
            proxy.Data = this;
        }
    }

    private void ButtonLoadThemes_OnClick(object sender, RoutedEventArgs e)
    {
        XamlThemeService.LoadThemeSource();
        XamlThemeService.LoadAllThemes();
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
        if (FindResource("ThemeSource") is CollectionViewSource source)
        {
            source?.View?.Refresh();
        }
    }

    private void ThemeSource_OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not KeyValuePair<string, ThemeInfo> kvp)
            return;
        var info = kvp.Value;
        if (!info.IsLocal && ViewModel.ThemeCategoryIndex == 1)
        {
            e.Accepted = false;
            return;
        }
        if (!info.IsAvailableOnMarket && ViewModel.ThemeCategoryIndex == 0)
        {
            e.Accepted = false;
            return;
        }

        var filter = ViewModel.ThemeFilterText;
        if (string.IsNullOrWhiteSpace(filter))
            return;
        e.Accepted = info.Manifest.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     info.Manifest.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     info.Manifest.Description.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        RequestRestart();
    }

    [RelayCommand]
    private void InstallTheme(string id)
    {
        XamlThemeService.RequestDownloadTheme(id);
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        await PluginMarketService.RefreshPluginSourceAsync();
    }

    [RelayCommand]
    private void OpenContextMenu(ContextMenu? menu)
    {
        if (menu != null)
        {
            menu.IsOpen = true;
        }
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
    private async void PackageThemeCommand(ThemeInfo? info)
    {
        if (info == null)
            return;
        var dialog = new SaveFileDialog()
        {
            Title = "打包主题",
            FileName = info.Manifest.Id + ".zip",
            Filter = $"ClassIsland 主题包(*.zip)|*.zip"
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;
        try
        {
            await XamlThemeService.PackageThemeAsync(info.Manifest.Id, dialog.FileName);
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.GetDirectoryName(dialog.FileName) ?? "",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            CommonDialog.ShowError($"无法打包主题 {info.Manifest.Id}：{ex.Message}");
        }
    }

    private void ButtonAgreeAgreement_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.IsThemeWarningVisible = false;
    }
}