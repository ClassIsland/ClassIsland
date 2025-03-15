using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class ManagementPolicySettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isLocked = true;
}