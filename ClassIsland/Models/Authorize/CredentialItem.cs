using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Authorize;

public partial class CredentialItem : ObservableObject
{
    [ObservableProperty] private string _providerId = "";

    [ObservableProperty] private object? _providerSettings;

    [ObservableProperty] private string _customName = "";

    [ObservableProperty] private bool _isCustomNameEnabled = false;
}