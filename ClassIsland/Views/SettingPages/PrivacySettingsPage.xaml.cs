using System;
using System.ComponentModel;
using System.Windows;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// PrivacySettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("privacy", "隐私", PackIconKind.ShieldAccountOutline, PackIconKind.ShieldAccount, SettingsPageCategory.Internal)]
public partial class PrivacySettingsPage : SettingsPageBase
{
    public SettingsService SettingsService { get; }

    public PrivacySettingsPage(SettingsService settingsService)
    {
        InitializeComponent();
        DataContext = this;
        SettingsService = settingsService;
        SettingsService.Settings.PropertyChanged += OnSettingsOnPropertyChanged;
    }

    private void OnSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SettingsService.Settings.IsSentryEnabled))
        {
            RequestRestart();
        }
    }

    private void HyperlinkMsAppCenter_OnClick(object sender, RoutedEventArgs e)
    {
        new DocumentReaderWindow()
        {
            Source = new Uri("/Assets/Documents/Privacy_.md", UriKind.RelativeOrAbsolute),
            Owner = Window.GetWindow(this),
            Title = "ClassIsland 隐私政策"
        }.ShowDialog();
    }

    private void PrivacySettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged += OnSettingsOnPropertyChanged;
    }

    private void PrivacySettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged -= OnSettingsOnPropertyChanged;
    }
}