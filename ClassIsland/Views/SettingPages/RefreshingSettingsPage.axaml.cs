using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.SettingPages;

[Group("classisland.general")]
[SettingsPageInfo("refreshing", "翻新与迎新", "\ue0b8", "\ue0b9", SettingsPageCategory.Internal)]
public partial class RefreshingSettingsPage : SettingsPageBase
{
    public RefreshingSettingsViewModel ViewModel { get; } = IAppHost.GetService<RefreshingSettingsViewModel>();
    
    public RefreshingSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void SettingsExpanderItemRefreshNow_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.RefreshingService.BeginRefresh(false);
    }

    private async void SettingsExpanderItemResetOnboardingMessages_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog()
        {
            Title = "重置迎新消息设置",
            Content = "确定要重置迎新消息设置吗？此操作不可撤销！",
            PrimaryButtonText = "重置",
            DefaultButton = ContentDialogButton.Primary,
            SecondaryButtonText = "取消"
        };
        var r = await dialog.ShowAsync(TopLevel.GetTopLevel(this));
        if (r != ContentDialogResult.Primary)
        {
            return;
        }

        ViewModel.SettingsService.Settings.OnboardingToastTitle = RefreshingService.DefaultOnboardingToastTitle;
        ViewModel.SettingsService.Settings.OnboardingToastBody = RefreshingService.DefaultOnboardingToastBody;
        this.ShowToast("已重置迎新消息。");
    }

    private void SettingsExpanderItemTestOnboardingDialog_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.RefreshingService.ShowOnboardingDialog(true);
    }

    private async void ButtonReserveSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        var win = new RefreshingScopesConfigDialog()
        {
            Scopes = ViewModel.SettingsService.Settings.RefreshingScopes
        };
        await win.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }
}