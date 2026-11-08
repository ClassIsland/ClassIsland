using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Controls.ProfileTransferProviders;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Enums.Profile;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Profile;
using ClassIsland.Shared;
using ClassIsland.ViewModels.MainViewParts;

namespace ClassIsland.Views.MainViewParts;

public partial class ImportExportProviderView : ViewBase
{
    private readonly IManagementService _managementService = IAppHost.GetService<IManagementService>();
    private readonly ProfileTransferProviderInfo _info;
    private readonly ProfileTransferProviderControlBase _control;
    private readonly ImportExportProviderViewModel _viewModel;

    public ImportExportProviderView(ProfileTransferProviderInfo info,
        ProfileTransferProviderControlBase control)
    {
        _info = info;
        _control = control;
        _viewModel = new ImportExportProviderViewModel(info, control);
        InitializeComponent();
        DataContext = _viewModel;
        Closed += (_, _) =>
        {
            if (_control is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
    }

    private async void InvokeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsBusy || !_managementService.Policy.CanEditProfile)
        {
            return;
        }

        _viewModel.IsBusy = true;
        try
        {
            if (!await _managementService.AuthorizeByLevel(_managementService.CredentialConfig.EditProfileAuthorizeLevel))
            {
                return;
            }

            if (_info.Type == ProfileTransferProviderType.Import && _viewModel.IsProfileTransferInvoked &&
                !await ContentDialogHelper.ShowConfirmationDialog("要继续导入吗",
                    "您先前已经成功地导入了档案，您还要继续导入吗？", root: TopLevel, positiveText: "继续"))
            {
                return;
            }

            var success = await _control.InvokeTransfer();
            if (success)
            {
                _viewModel.IsProfileTransferInvoked = true;
            }
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "导入/导出失败", exception);
        }
        finally
        {
            _viewModel.IsBusy = false;
        }
    }
}
