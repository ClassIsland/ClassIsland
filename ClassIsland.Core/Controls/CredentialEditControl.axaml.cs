using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;

namespace ClassIsland.Core.Controls;

/// <summary>
/// CredentialEditControl.xaml 的交互逻辑
/// </summary>
public partial class CredentialEditControl : UserControl
{
    
    /// <summary>
    /// 认证字符串的依赖属性
    /// </summary>
    public static readonly StyledProperty<string?> CredentialStringProperty = AvaloniaProperty.Register<CredentialEditControl, string?>(
        nameof(CredentialString));

    /// <summary>
    /// 认证字符串
    /// </summary>
    public string? CredentialString
    {
        get => GetValue(CredentialStringProperty);
        set => SetValue(CredentialStringProperty, value);
    }

    /// <summary>
    /// 创建一个新的 <see cref="CredentialEditControl"/> 实例。
    /// </summary>
    public CredentialEditControl()
    {
        InitializeComponent();
    }

    private async void ButtonEditCredentialString_OnClick(object sender, RoutedEventArgs e)
    {
        var authorizeService = IAppHost.GetService<IAuthorizeService>();
        CredentialString = await authorizeService.SetupCredentialStringAsync(string.IsNullOrWhiteSpace(CredentialString) ? null : CredentialString, TopLevel.GetTopLevel(this) as Window);
    }

    private void ButtonClearCredentialString_OnClick(object sender, RoutedEventArgs e)
    {
        CredentialString = "";
    }
}
