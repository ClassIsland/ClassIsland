using Avalonia.Controls;
using ClassIsland.Core.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.MainViewParts;

internal sealed class ImportExportProviderViewModel(
    ProfileTransferProviderInfo info,
    Control providerContent) : ObservableObject
{
    private bool _isBusy;

    public string Title => info.HidePageTitle ? "" : info.Name;
    public Control ProviderContent { get; } = providerContent;
    public bool IsProfileTransferInvoked { get; set; }

    public string InvokeButtonText => info.Type switch
    {
        Core.Enums.Profile.ProfileTransferProviderType.Import => "导入",
        Core.Enums.Profile.ProfileTransferProviderType.Export => "导出",
        _ => "执行"
    };

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
}
