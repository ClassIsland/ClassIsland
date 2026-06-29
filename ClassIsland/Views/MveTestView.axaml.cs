using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Enums.UI;
using ClassIsland.Core.Services.UI;

namespace ClassIsland.Views;

public partial class MveTestView : ViewBase
{
    public MveTestView()
    {
        InitializeComponent();
    }

    private void ButtonOpenViewDefault_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = ViewManagementService.Instance.ActivateNewView<MveTestView>();
        view.Show();
    }

    private void ButtonOpenViewNewViewHost_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = ViewManagementService.Instance.ActivateNewView<MveTestView>(activationPreference: ViewActivationPreference.NewViewHost);
        view.Show();
    }

    private void ButtonOpenViewExistedViewHost_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = ViewManagementService.Instance.ActivateNewView<MveTestView>(activationPreference: ViewActivationPreference.ExistedViewHost);
        view.Show();
    }
}