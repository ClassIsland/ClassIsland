using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Models.Authorize;
using ClassIsland.Views;

namespace ClassIsland.Controls;

/// <summary>
/// AuthorizeProviderPresenter.xaml 的交互逻辑
/// </summary>
public partial class AuthorizeProviderPresenter : UserControl
{
    #region Events

    public static readonly RoutedEvent<RequestValidateAuthorizationProviderEventArgs> RequestValidateAuthorizationProvidersEvent =
        RoutedEvent.Register<AuthorizeProviderPresenter, RequestValidateAuthorizationProviderEventArgs>(nameof(RequestValidateAuthorizationProviders), RoutingStrategies.Direct);
    
    public event EventHandler<RequestValidateAuthorizationProviderEventArgs> RequestValidateAuthorizationProviders
    { 
        add => AddHandler(RequestValidateAuthorizationProvidersEvent, value);
        remove => RemoveHandler(RequestValidateAuthorizationProvidersEvent, value);
    }

    #endregion
    
    #region Props
    

    public static readonly StyledProperty<CredentialItem?> CredentialItemProperty = AvaloniaProperty.Register<AuthorizeProviderPresenter, CredentialItem?>(
        nameof(CredentialItem));

    public CredentialItem? CredentialItem
    {
        get => GetValue(CredentialItemProperty);
        set => SetValue(CredentialItemProperty, value);
    }

    public static readonly StyledProperty<AuthorizeProviderControlBase?> DisplayingContentProperty = AvaloniaProperty.Register<AuthorizeProviderPresenter, AuthorizeProviderControlBase?>(
        nameof(DisplayingContent));

    public AuthorizeProviderControlBase? DisplayingContent
    {
        get => GetValue(DisplayingContentProperty);
        set => SetValue(DisplayingContentProperty, value);
    }

    public static readonly StyledProperty<bool> IsEditingModeProperty = AvaloniaProperty.Register<AuthorizeProviderPresenter, bool>(
        nameof(IsEditingMode));

    public bool IsEditingMode
    {
        get => GetValue(IsEditingModeProperty);
        set => SetValue(IsEditingModeProperty, value);
    }

    #endregion

    private AuthorizeWindow? _window;

    private void UpdateContent()
    {
        var item = CredentialItem;
        if (item == null)
        {
            return;
        }
        var info = AuthorizeProviderRegistryService.RegisteredAuthorizeProviders.FirstOrDefault(x =>
            x.Id == item.ProviderId);
        var settings = item.ProviderSettings;
        if (info == null)
        {
            return;
        }

        var visual = AuthorizeProviderControlBase.GetInstance(info, ref settings, IsEditingMode);
        item.ProviderSettings = settings;
        if (visual == null)
        {
            return;
        }
        
        DisplayingContent = visual;
    }

    public AuthorizeProviderPresenter()
    {
        InitializeComponent();
        this.GetObservable(CredentialItemProperty).Skip(1).Subscribe(_ => UpdateContent());
        this.GetObservable(IsEditingModeProperty).Skip(1).Subscribe(_ => UpdateContent());
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _window = TopLevel.GetTopLevel(this) as AuthorizeWindow;
        _window?.AddHandler(RequestValidateAuthorizationProvidersEvent, RequestValidateAuthorizationProvidersEventHandler);
    }
    
    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _window?.RemoveHandler(RequestValidateAuthorizationProvidersEvent, RequestValidateAuthorizationProvidersEventHandler);
        _window = null;
    }

    private void RequestValidateAuthorizationProvidersEventHandler(object? sender, RequestValidateAuthorizationProviderEventArgs args)
    {
        if (args.IsError || DisplayingContent == null)
        {
            return;
        }

        args.IsError = !DisplayingContent.ValidateAuthorizeSettings();
    }
    
}
