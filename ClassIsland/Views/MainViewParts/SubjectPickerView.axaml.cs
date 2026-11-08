using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels.MainViewParts;

namespace ClassIsland.Views.MainViewParts;

public partial class SubjectPickerView : ViewBase
{
    private readonly IProfileService _profileService = IAppHost.GetService<IProfileService>();
    private readonly IManagementService _managementService = IAppHost.GetService<IManagementService>();
    private readonly Action<Guid> _onSelected;
    private readonly SubjectPickerViewModel _viewModel;
    private bool _isSelecting;

    public SubjectPickerView(Action<Guid> onSelected)
    {
        _onSelected = onSelected;
        _viewModel = new SubjectPickerViewModel(_profileService);
        InitializeComponent();
        DataContext = _viewModel;
        Closed += SubjectPickerView_OnClosed;
    }

    private void SubjectPickerView_OnClosed(object? sender, RoutedEventArgs e)
    {
        _viewModel.Dispose();
        Closed -= SubjectPickerView_OnClosed;
    }

    private void SubjectList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSelecting || SubjectList.SelectedItem is not KeyValuePair<Guid, Subject> pair)
        {
            return;
        }

        _isSelecting = true;
        _onSelected(pair.Key);
        _isSelecting = false;
        Hide();
    }

    private async void CreateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (AssociatedViewHost == null || !_managementService.Policy.CanEditProfile ||
            !await _managementService.AuthorizeByLevel(_managementService.CredentialConfig.EditProfileAuthorizeLevel))
        {
            return;
        }

        var id = Guid.NewGuid();
        var view = new SubjectEditView(id, true, _viewModel.SearchText.Trim());
        view.Saved += () =>
        {
            var subject = _profileService.Profile.Subjects.GetValueOrDefault(id);
            if (subject == null)
            {
                return;
            }

            _viewModel.SearchText = subject.Name;
        };
        await AssociatedViewHost.PushDrawerView(view);
    }

    private async void MoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control anchor || AssociatedViewHost is not { } host)
        {
            return;
        }

        var menu = new MenuFlyout();
        var manage = new MenuItem
        {
            Header = "管理科目…",
            Icon = new FluentIcon(Core.Icons.FluentIcons.BookOpenRegular)
        };
        manage.Click += async (_, _) =>
        {
            if (!_managementService.Policy.CanEditProfile || !await _managementService.AuthorizeByLevel(_managementService.CredentialConfig.EditProfileAuthorizeLevel))
            {
                return;
            }

            await host.HideDrawer();
            var view = new SubjectManagementView();
            if (host.ActivateView(view))
            {
                await host.ShowView(view);
            }
        };
        menu.Items.Add(manage);
        menu.ShowAt(anchor);
    }
}
