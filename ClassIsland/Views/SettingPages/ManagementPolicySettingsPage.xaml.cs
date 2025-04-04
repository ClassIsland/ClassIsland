using System.Windows;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// ManagementPolicySettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("management.policy", "集控策略", true, SettingsPageCategory.About)]
public partial class ManagementPolicySettingsPage
{
    public IManagementService ManagementService { get; }
    public ManagementPolicySettingsViewModel ViewModel { get; } = new();

    public ManagementPolicySettingsPage(IManagementService managementService)
    {
        ManagementService = managementService;
        DataContext = this;
        InitializeComponent();
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart();
    }

    private async void ManagementPolicySettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ManagementService.IsManagementEnabled)
        {
            return;
        }
        var result =
            await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig
                .EditPolicyAuthorizeLevel);
        if (result)
        {
            ViewModel.IsLocked = false;
        }
    }

    private void ManagementPolicySettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsLocked = true;
    }
}