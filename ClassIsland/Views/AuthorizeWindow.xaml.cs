using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Models;
using ClassIsland.Models.Authorize;
using ClassIsland.ViewModels;
using AuthorizeProviderDisplayingModel = ClassIsland.Models.Authorize.AuthorizeProviderDisplayingModel;

namespace ClassIsland.Views;

/// <summary>
/// AuthorizeWindow.xaml 的交互逻辑
/// </summary>
public partial class AuthorizeWindow
{
    public AuthorizeViewModel ViewModel { get; } = new();

    public AuthorizeWindow(Credential credential, bool isEditingMode)
    {
        DataContext = this;
        ViewModel.Credential = credential;
        ViewModel.IsEditingMode = isEditingMode;
        InitializeComponent();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        var result = SetWindowDisplayAffinity((HWND)new WindowInteropHelper(this).Handle, WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE);
        base.OnContentRendered(e);
    }

    private AuthorizeProviderDisplayingModel? GetDisplayingModel(CredentialItem item)
    {
        var info = AuthorizeProviderRegistryService.RegisteredAuthorizeProviders.FirstOrDefault(x =>
            x.Id == item.ProviderId);
        var settings = item.ProviderSettings;
        if (info == null)
        {
            return null;
        }

        var visual = AuthorizeProviderControlBase.GetInstance(info, ref settings, ViewModel.IsEditingMode);
        item.ProviderSettings = settings;
        if (visual == null)
        {
            return null;
        }
        return new AuthorizeProviderDisplayingModel(info, visual, item);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        foreach (var i in ViewModel.Credential.Items)
        {
            var model = GetDisplayingModel(i);
            if (model != null)
            {
                ViewModel.Providers.Add(model);
            }
        }
    }

    private void ButtonAddAuthorizeMethod_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedAuthorizeProviderInfo == null)
        {
            return;
        }

        var item = new CredentialItem()
        {
            ProviderId = ViewModel.SelectedAuthorizeProviderInfo.Id
        };
        ViewModel.Credential.Items.Add(item);
        var model = GetDisplayingModel(item);
        if (model != null)
        {
            ViewModel.Providers.Add(model);
        }
    }

    private void ButtonRemoveSelectedAuthProvider_OnClick(object sender, RoutedEventArgs e)
    {
        var item = ViewModel.SelectedDisplayingInfo;
        if (item == null)
        {
            return;
        }

        ViewModel.Credential.Items.Remove(item.AssociatedCredentialItem);
        ViewModel.Providers.Remove(item);
    }

    private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsEditingMode)
        {
            return;
        }
        DialogResult = true;
        Close();
    }

    private void CommandBindingCompleteAuthorize_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (ViewModel.IsEditingMode)
        {
            return;
        }
        DialogResult = true;
        Close();
    }
}