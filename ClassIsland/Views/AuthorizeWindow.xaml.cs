using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Models.Authorize;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.Logging;

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
        Loaded += (sender, args) => ViewModel.SelectedCredentialItem = ViewModel.Credential.Items.FirstOrDefault();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        var result = SetWindowDisplayAffinity((HWND)new WindowInteropHelper(this).Handle, WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE);
        base.OnContentRendered(e);
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
        ViewModel.SelectedCredentialItem = item;
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

        if (ViewModel.Credential.Items.Count <= 0)
        {
            CommonDialog.ShowError("请至少添加一个认证方式。");
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