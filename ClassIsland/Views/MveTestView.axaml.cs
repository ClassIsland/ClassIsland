using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Enums.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Core.Services.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class MveTestView : ViewBase
{
    private ViewBase? _lastView;
    
    public MveTestView()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, ViewClosingEventArgs e)
    {
        if (Confirm.IsChecked != true || !e.IsCancelable)
        {
            return;    
        }

        e.Cancel = true;
        this.ShowToast("关闭事件已拦截。");
    }

    private async void ShowView(ViewBase view)
    {
        if (ShowModal.IsChecked == true)
        {
            await view.ShowModal(this);
        }
        else
        {
            view.Show();
        }
        this.ShowSuccessToast("Done.");
    }

    private void ButtonOpenViewDefault_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = _lastView = new MveTestView();
        ShowView(view);
    }

    private void ButtonOpenViewNewViewHost_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = _lastView = ViewManagementService.Instance.ActivateNewView<MveTestView>(activationPreference: ViewActivationPreference.NewViewHost);
        ShowView(view);
    }

    private void ButtonOpenViewExistedViewHost_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = _lastView = ViewManagementService.Instance.ActivateNewView<MveTestView>(activationPreference: ViewActivationPreference.ExistedViewHost);
        ShowView(view);
    }

    private void ButtonReuseLastView_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_lastView != null) 
            ShowView(_lastView);
    }

    private void ButtonHideLastView_OnClick(object? sender, RoutedEventArgs e)
    {
        _lastView?.Hide();
    }

    private void ButtonTestBigImage_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = _lastView = new MveBigImageTestView();
        ShowView(view);
    }

    private void ButtonOpenLastView_OnClick(object? sender, RoutedEventArgs e)
    {
        _lastView?.Open();
    }
}