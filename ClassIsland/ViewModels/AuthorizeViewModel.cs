using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Authorize;
using CommunityToolkit.Mvvm.ComponentModel;
using AuthorizeProviderDisplayingModel = ClassIsland.Models.Authorize.AuthorizeProviderDisplayingModel;

namespace ClassIsland.ViewModels;

public partial class AuthorizeViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<AuthorizeProviderDisplayingModel> _providers = [];

    [ObservableProperty] private bool _isEditingMode = false;

    [ObservableProperty] private Credential _credential = new();

    [ObservableProperty] private AuthorizeProviderInfo? _selectedAuthorizeProviderInfo;

    [ObservableProperty] private CredentialItem? _selectedCredentialItem;
}