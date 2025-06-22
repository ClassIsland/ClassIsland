using System;
using System.Windows;
using Avalonia;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.SettingsWindow;
using FluentAvalonia.UI.Navigation;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("test-settings-page", "测试页面", SettingsPageCategory.Debug)]
public partial class TestSettingsPage : SettingsPageBase
{
    private string _navigationUri;

    public static readonly DirectProperty<TestSettingsPage, string> NavigationUriProperty = AvaloniaProperty.RegisterDirect<TestSettingsPage, string>(
        nameof(NavigationUri), o => o.NavigationUri, (o, v) => o.NavigationUri = v);

    public string NavigationUri
    {
        get => _navigationUri;
        set => SetAndRaise(NavigationUriProperty, ref _navigationUri, value);
    }

    public TestSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
    }

    // private void OnLoaded(object sender, RoutedEventArgs e)
    // {
    //     var navigationService = NavigationService.GetNavigationService(this);
    //     navigationService!.Navigated += OnNavigated;
    // }
    //
    // private void OnNavigated(object sender, NavigationEventArgs args)
    // {
    //     NavigationUri = (args.ExtraData as SettingsWindowNavigationData)?.NavigateUri;
    // }
    //
    // private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    // {
    //     NavigationService!.Navigate(new Page());
    // }
    //
    // private void TestSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    // {
    //     var navigationService = NavigationService.GetNavigationService(this);
    //     navigationService!.Navigated -= OnNavigated;
    // }
}

