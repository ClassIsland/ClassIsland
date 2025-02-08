using System.Linq;
using System.Text.Json.Serialization;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Services.Registry;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Authorize;

public partial class CredentialItem : ObservableObject
{
    [ObservableProperty] private string _providerId = "";

    [ObservableProperty] private object? _providerSettings;

    [ObservableProperty] private string _customName = "";

    [ObservableProperty] private bool _isCustomNameEnabled = false;

    [JsonIgnore]
    public AuthorizeProviderInfo? ProviderInfo =>
        AuthorizeProviderRegistryService.RegisteredAuthorizeProviders.FirstOrDefault(x =>
            x.Id == ProviderId);
}