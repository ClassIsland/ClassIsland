using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public partial class NuGetLicenseInfo : ObservableObject
{
    [ObservableProperty]
    private string _packageId = string.Empty;

    [ObservableProperty]
    private string _packageVersion = string.Empty;

    [ObservableProperty]
    private string? _packageProjectUrl;

    [ObservableProperty]
    private string _authors = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string? _license;

    [ObservableProperty]
    private string? _licenseUrl;

    [ObservableProperty]
    private int _licenseInformationOrigin;
}