using System;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Controls.ProfileTransferProviders;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Models.Profile;
using ClassIsland.Shared;
using ClassIsland.ViewModels.MainViewParts;

namespace ClassIsland.Views.MainViewParts;

public partial class ImportExportView : ViewBase
{
    private readonly IManagementService _managementService = IAppHost.GetService<IManagementService>();
    private readonly ImportExportViewModel _viewModel;
    private bool _isOpening;

    public ImportExportView()
    {
        _viewModel = new ImportExportViewModel();
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void ProviderList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isOpening || sender is not ListBox { SelectedItem: ProfileTransferProviderInfo info } list ||
            AssociatedViewHost == null || !_managementService.Policy.CanEditProfile)
        {
            return;
        }

        list.SelectedItem = null;
        _isOpening = true;
        try
        {
            if (!await _managementService.AuthorizeByLevel(_managementService.CredentialConfig.EditProfileAuthorizeLevel))
            {
                return;
            }

            if (info.FunctionHandler != null)
            {
                info.FunctionHandler(TopLevel!);
                return;
            }
            if (info.HandlerControlType == null ||
                Activator.CreateInstance(info.HandlerControlType) is not ProfileTransferProviderControlBase control)
            {
                return;
            }

            await AssociatedViewHost.PushDrawerView(new ImportExportProviderView(info, control));
        }
        finally
        {
            _isOpening = false;
        }
    }
}
