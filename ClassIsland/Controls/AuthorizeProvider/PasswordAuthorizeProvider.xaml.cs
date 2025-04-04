using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.AuthorizeProvider;

/// <summary>
/// PasswordAuthorizeProvider.xaml 的交互逻辑
/// </summary>
[AuthorizeProviderInfo("classisland.authProviders.password", "密码", PackIconKind.Password)]
public partial class PasswordAuthorizeProvider
{
    public static readonly DependencyProperty AuthorizeFailedProperty = DependencyProperty.Register(
        nameof(AuthorizeFailed), typeof(bool), typeof(PasswordAuthorizeProvider), new PropertyMetadata(default(bool)));

    public bool AuthorizeFailed
    {
        get { return (bool)GetValue(AuthorizeFailedProperty); }
        set { SetValue(AuthorizeFailedProperty, value); }
    }

    public static readonly DependencyProperty ProtectPasswordProperty = DependencyProperty.Register(
        nameof(ProtectPassword), typeof(bool), typeof(PasswordAuthorizeProvider), new PropertyMetadata(default(bool)));

    public bool ProtectPassword
    {
        get { return (bool)GetValue(ProtectPasswordProperty); }
        set { SetValue(ProtectPasswordProperty, value); }
    }

    public PasswordAuthorizeProvider()
    {
        InitializeComponent();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (IsEditingMode)
        {
            UpdatePassword();
        }
    }

    private void UpdatePassword()
    {
        var password = PasswordBox.Password;
        var saltBytes = RandomNumberGenerator.GetBytes(16).ToList();
        var passwordBytes = Encoding.UTF8.GetBytes(password).ToList();
        passwordBytes.AddRange(saltBytes);
        var hash = SHA256.HashData(passwordBytes.ToArray());
        Settings.PasswordSalt = saltBytes.ToArray();
        Settings.PasswordHash = Convert.ToBase64String(hash);
    }

    private void ButtonCheckPassword_OnClick(object sender, RoutedEventArgs e)
    {
        CheckPassword();
    }

    private void CheckPassword()
    {
        var password = PasswordBox.Password;
        var saltBytes = Settings.PasswordSalt;
        var passwordBytes = Encoding.UTF8.GetBytes(password).ToList();
        passwordBytes.AddRange(saltBytes);
        var hash = SHA256.HashData(passwordBytes.ToArray());
        if (Convert.ToBase64String(hash) == Settings.PasswordHash)
        {
            CompleteAuthorize();
        }
        else
        {
            AuthorizeFailed = true;
        }
    }

    private void PasswordBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (IsEditingMode || e.Key != Key.Enter)
        {
            return;
        }
        CheckPassword();
    }

    private void PasswordAuthorizeProvider_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Settings.PasswordHash) && IsEditingMode)
        {
            ProtectPassword = true;
        }

        var parentItem = VisualTreeUtils.FindParentVisuals<ListBoxItem>(this).FirstOrDefault();
        if (parentItem?.IsSelected == true)
        {
            PasswordBox.Focus();
        }
    }

    private void ButtonChangePassword_OnClick(object sender, RoutedEventArgs e)
    {
        ProtectPassword = false;
        PasswordBox.Focus();
    }
}