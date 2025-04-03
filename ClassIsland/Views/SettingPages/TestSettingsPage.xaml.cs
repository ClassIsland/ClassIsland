using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.SettingsWindow;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("test-settings-page", "测试页面", true, SettingsPageCategory.Debug)]
public partial class TestSettingsPage : SettingsPageBase
{
    public static readonly DependencyProperty NavigationUriProperty = DependencyProperty.Register(
        nameof(NavigationUri), typeof(Uri), typeof(TestSettingsPage), new PropertyMetadata(default(Uri)));

    public Uri? NavigationUri
    {
        get { return (Uri)GetValue(NavigationUriProperty); }
        set { SetValue(NavigationUriProperty, value); }
    }

    public TestSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
        Loaded+= OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var navigationService = NavigationService.GetNavigationService(this);
        navigationService!.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs args)
    {
        NavigationUri = (args.ExtraData as SettingsWindowNavigationData)?.NavigateUri;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        NavigationService!.Navigate(new Page());
    }

    private void TestSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        var navigationService = NavigationService.GetNavigationService(this);
        navigationService!.Navigated -= OnNavigated;
    }
}