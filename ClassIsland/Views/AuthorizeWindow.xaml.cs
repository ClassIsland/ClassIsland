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
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.Logging;
using AuthorizeProviderDisplayingModel = ClassIsland.Models.Authorize.AuthorizeProviderDisplayingModel;

namespace ClassIsland.Views;

/// <summary>
/// AuthorizeWindow.xaml 的交互逻辑
/// </summary>
public partial class AuthorizeWindow
{
    public AuthorizeViewModel ViewModel { get; } = new();

    private ILogger<AuthorizeWindow> Logger { get; } = IAppHost.GetService<ILogger<AuthorizeWindow>>();

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
    }

    private void ButtonRemoveSelectedAuthProvider_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedCredentialItem != null)
            ViewModel.Credential.Items.Remove(ViewModel.SelectedCredentialItem);
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
            Logger.LogWarning("来自 {} 的命令 CommandBindingCompleteAuthorize 在编辑模式下没有效果，已忽略此调用。", sender.GetType());
            return;
        }
        DialogResult = true;
        Close();
    }
}