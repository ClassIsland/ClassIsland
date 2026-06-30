using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Enums.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Services.UI;

namespace ClassIsland.Views;

public partial class MveTestView : ViewBase
{
    public MveTestView()
    {
        InitializeComponent();
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
        var view = ViewManagementService.Instance.ActivateNewView<MveTestView>();
        ShowView(view);
    }

    private void ButtonOpenViewNewViewHost_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = ViewManagementService.Instance.ActivateNewView<MveTestView>(activationPreference: ViewActivationPreference.NewViewHost);
        ShowView(view);
    }

    private void ButtonOpenViewExistedViewHost_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = ViewManagementService.Instance.ActivateNewView<MveTestView>(activationPreference: ViewActivationPreference.ExistedViewHost);
        ShowView(view);
    }
}