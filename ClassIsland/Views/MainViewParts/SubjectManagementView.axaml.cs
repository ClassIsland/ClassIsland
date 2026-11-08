using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels.MainViewParts;

namespace ClassIsland.Views.MainViewParts;

public partial class SubjectManagementView : ViewBase
{
    private readonly SubjectManagementViewModel _viewModel;
    private bool _isOpening;

    public SubjectManagementView()
    {
        _viewModel = new SubjectManagementViewModel(IAppHost.GetService<IProfileService>());
        InitializeComponent();
        DataContext = _viewModel;
        Closed += SubjectManagementView_OnClosed;
    }

    private void SubjectManagementView_OnClosed(object? sender, RoutedEventArgs e)
    {
        _viewModel.Dispose();
        Closed -= SubjectManagementView_OnClosed;
    }

    private async void SubjectList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isOpening || SubjectList.SelectedItem is not KeyValuePair<Guid, Subject> pair ||
            AssociatedViewHost == null)
        {
            return;
        }

        _isOpening = true;
        SubjectList.SelectedItem = null;
        var view = new SubjectEditView(pair.Key);
        try
        {
            await AssociatedViewHost.ShowDrawer(view);
        }
        finally
        {
            _isOpening = false;
        }
    }

    private async void AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        var id = Guid.NewGuid();
        var view = new SubjectEditView(id, true);
        await AssociatedViewHost.ShowDrawer(view);
    }
}
