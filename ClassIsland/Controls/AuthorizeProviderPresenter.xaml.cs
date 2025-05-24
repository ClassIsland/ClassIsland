using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Models.Authorize;

namespace ClassIsland.Controls;

/// <summary>
/// AuthorizeProviderPresenter.xaml 的交互逻辑
/// </summary>
public partial class AuthorizeProviderPresenter : UserControl
{
    #region Props

    
    public static readonly DependencyProperty CredentialItemProperty = DependencyProperty.Register(
        nameof(CredentialItem), typeof(CredentialItem), typeof(AuthorizeProviderPresenter), new PropertyMetadata(default(CredentialItem), (o, args) =>
        {
            if (o is AuthorizeProviderPresenter control)
            {
                control.UpdateContent();
            }
        }));

    public CredentialItem? CredentialItem
    {
        get { return (CredentialItem)GetValue(CredentialItemProperty); }
        set { SetValue(CredentialItemProperty, value); }
    }

    public static readonly DependencyProperty DisplayingContentProperty = DependencyProperty.Register(
        nameof(DisplayingContent), typeof(object), typeof(AuthorizeProviderPresenter), new PropertyMetadata(default(object)));

    public object DisplayingContent
    {
        get { return (object)GetValue(DisplayingContentProperty); }
        set { SetValue(DisplayingContentProperty, value); }
    }

    public static readonly DependencyProperty IsEditingModeProperty = DependencyProperty.Register(
        nameof(IsEditingMode), typeof(bool), typeof(AuthorizeProviderPresenter), new PropertyMetadata(default(bool),
            (o, args) =>
            {
                if (o is AuthorizeProviderPresenter control)
                {
                    control.UpdateContent();
                }
            }));

    public bool IsEditingMode
    {
        get { return (bool)GetValue(IsEditingModeProperty); }
        set { SetValue(IsEditingModeProperty, value); }
    }

    #endregion

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
    }
}