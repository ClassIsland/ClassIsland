using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.MainView;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Icons;
using ClassIsland.Shared;

namespace ClassIsland.Views.MainViewPages;

[MainViewPageInfo("classisland.more", "更多", FluentIcons.MoreHorizontalRegular, MainViewPageCategory.Footer)]
public partial class MoreMainViewPage : MainViewPageBase
{
    private MainView Owner => VisualExtensions.FindAncestorOfType<MainView>(this)!;

    public MoreMainViewPage()
    {
        InitializeComponent();
    }

    private void ButtonSettings_OnClick(object sender, RoutedEventArgs e)
    {
        IAppHost.GetService<ProfileSettingsWindow>().Open();
    }

    private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<SettingsWindowNew>().Open();
    }

    private void MenuItemLiveActivitySettings_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<SettingsWindowNew>().Open("live-activity");
    }


    private async void MenuItemExitApp_OnClick(object sender, RoutedEventArgs e)
    {
        var owner = Owner;
        var management = owner.ViewModel.ManagementService;
        if (!await management.AuthorizeByLevel(management.CredentialConfig.ExitApplicationAuthorizeLevel))
        {
            return;
        }

        if (PlatformHelper.IsAppleMobile)
        {
            ((App)AppBase.Current).PrepareForAppleMobileManualTermination();
            return;
        }

        owner.Close();
        AppBase.Current.Stop();
    }
    private void MenuItemRestartApp_OnClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart();
    }

    private void MenuItemTemporaryClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var window = App.GetService<ProfileSettingsWindow>();
        window.OpenDrawer("TemporaryClassPlan");
        window.Open();
    }

    private void MenuItemClassSwap_OnClick(object sender, RoutedEventArgs e)
    {
        Owner.OpenClassSwapWindow();
    }

    private void FASettingsExpanderDevPortal_OnClick(object? sender, RoutedEventArgs e)
    {
        new DevPortalWindow().Show();
    }
}
