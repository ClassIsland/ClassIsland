using System.Diagnostics;
using System.Windows;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// ThemesSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("classisland.themes", "主题", PackIconKind.FileCodeOutline, PackIconKind.FileCode)]
public partial class ThemesSettingsPage
{
    public IXamlThemeService XamlThemeService { get; }

    public ThemesSettingsPage(IXamlThemeService xamlThemeService)
    {
        XamlThemeService = xamlThemeService;
        InitializeComponent();
        DataContext = this;
    }

    private void ButtonLoadThemes_OnClick(object sender, RoutedEventArgs e)
    {
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
}