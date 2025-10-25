using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.AuthorizeProviderSettings;

namespace ClassIsland.Controls.AuthorizeProvider;

/// <summary>
/// PasswordAuthorizeProvider.xaml 的交互逻辑
/// </summary>
[AuthorizeProviderInfo("classisland.authProviders.password", "密码", "\uec8b")]
public partial class PasswordAuthorizeProvider : AuthorizeProviderControlBase<PasswordAuthorizeSettings>
{
    public static readonly StyledProperty<bool> AuthorizeFailedProperty = AvaloniaProperty.Register<PasswordAuthorizeProvider, bool>(
        nameof(AuthorizeFailed));

    public bool AuthorizeFailed
    {
        get => GetValue(AuthorizeFailedProperty);
        set => SetValue(AuthorizeFailedProperty, value);
    }

    public static readonly StyledProperty<bool> ProtectPasswordProperty = AvaloniaProperty.Register<PasswordAuthorizeProvider, bool>(
        nameof(ProtectPassword));

    public bool ProtectPassword
    {
        get => GetValue(ProtectPasswordProperty);
        set => SetValue(ProtectPasswordProperty, value);
    }

    public static readonly StyledProperty<bool> ConfirmPasswordUnmatchProperty = AvaloniaProperty.Register<PasswordAuthorizeProvider, bool>(
        nameof(ConfirmPasswordUnmatch));

    public bool ConfirmPasswordUnmatch
    {
        get => GetValue(ConfirmPasswordUnmatchProperty);
        set => SetValue(ConfirmPasswordUnmatchProperty, value);
    }

    public static readonly StyledProperty<bool> PasswordEmptyErrorProperty = AvaloniaProperty.Register<PasswordAuthorizeProvider, bool>(
        nameof(PasswordEmptyError));

    public bool PasswordEmptyError
    {
        get => GetValue(PasswordEmptyErrorProperty);
        set => SetValue(PasswordEmptyErrorProperty, value);
    }

    public PasswordAuthorizeProvider()
    {
        InitializeComponent();
    }

    private void PasswordBox_OnPasswordChanged(object? sender, TextChangedEventArgs textChangedEventArgs)
    {
        if (IsEditingMode)
        {
            UpdatePassword();
        }
    }

    private void UpdatePassword()
    {
        var password = PasswordBox.Text ?? "";
        if (string.IsNullOrEmpty(password))
        {
            PasswordEmptyError = true;
            return;
        }
        PasswordEmptyError = false;
        if (password != PasswordBoxConfirm.Text)
        {
            return;
        }
        
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
        AuthorizeFailed = false;
        var password = PasswordBox.Text ?? "";
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
            IsEnabled = false;
            DispatcherTimer.RunOnce(() =>
                {
                    IsEnabled = true;
                    AuthorizeFailed = true;
                },
                TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(1000, 3000)));
        }
    }

    private void PasswordBox_OnKeyDown(object? sender, KeyEventArgs e)
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

        var parentItem = this.FindAncestorOfType<ListBoxItem>();
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

    public override bool ValidateAuthorizeSettings()
    {
        if (ProtectPassword)
        {
            // 实际上没有进行过编辑操作
            return true;
        }

        if (string.IsNullOrEmpty(PasswordBox.Text))
        {
            PasswordEmptyError = true;
            return false;
        }
        ConfirmPasswordUnmatch = PasswordBoxConfirm.Text != PasswordBox.Text;
        return !ConfirmPasswordUnmatch;
    }

    private void PasswordBoxConfirm_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        ConfirmPasswordUnmatch = PasswordBoxConfirm.Text != PasswordBox.Text;
    }
}
