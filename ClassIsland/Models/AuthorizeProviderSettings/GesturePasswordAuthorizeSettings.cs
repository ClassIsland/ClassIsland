using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AuthorizeProviderSettings;

public partial class GesturePasswordAuthorizeSettings : ObservableObject
{
    [ObservableProperty]
    private string _gestureHash = "";

    [ObservableProperty]
    private byte[] _gestureSalt = [];
}
