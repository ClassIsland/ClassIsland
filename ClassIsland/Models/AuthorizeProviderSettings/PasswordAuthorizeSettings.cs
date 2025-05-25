using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AuthorizeProviderSettings;

public partial class PasswordAuthorizeSettings : ObservableObject
{
    [ObservableProperty]
    private string _passwordHash = "";

    [ObservableProperty]
    private byte[] _passwordSalt = [];
}