using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Models.Authorize;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Views;

/// <summary>
/// AuthorizeWindow.xaml 的交互逻辑
/// </summary>
public partial class AuthorizeWindow : MyWindow
{
    public AuthorizeViewModel ViewModel { get; } = new();

    private ILogger<AuthorizeWindow> Logger { get; } = IAppHost.GetService<ILogger<AuthorizeWindow>>();

    public bool DialogResult { get; set; } = false;

    public AuthorizeWindow(Credential credential, bool isEditingMode)
    {
        DataContext = this;
        ViewModel.Credential = credential;
        ViewModel.IsEditingMode = isEditingMode;
        InitializeComponent();
        Loaded += (sender, args) => ViewModel.SelectedCredentialItem = ViewModel.Credential.Items.FirstOrDefault();
    }

    public override void Show()
    {
        base.Show();
        Activate();
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Private, true);
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
            this.ShowWarningToast("请至少添加一个认证方式。");
            return;
        }

        var eventArgs = new RequestValidateAuthorizationProviderEventArgs(this);
        RaiseEvent(eventArgs);
        if (eventArgs.IsError)
        {
            this.ShowErrorToast("认证方式的设置验证失败，请检查认证方式配置。");
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
