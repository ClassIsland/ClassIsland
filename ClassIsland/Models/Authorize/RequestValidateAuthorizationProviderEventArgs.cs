using Avalonia.Interactivity;
using ClassIsland.Controls;
using ClassIsland.Views;

namespace ClassIsland.Models.Authorize;

public class RequestValidateAuthorizationProviderEventArgs(object? source) : RoutedEventArgs(AuthorizeProviderPresenter.RequestValidateAuthorizationProvidersEvent, source)
{
    public bool IsError { get; set; } = false;
}