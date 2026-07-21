using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Controls.GesturePassword;
using ClassIsland.Models.AuthorizeProviderSettings;

namespace ClassIsland.Controls.AuthorizeProvider;

[AuthorizeProviderInfo("classisland.authProviders.gesturePassword", "手势密码", "\ue770")]
public partial class GesturePasswordAuthorizeProvider : AuthorizeProviderControlBase<GesturePasswordAuthorizeSettings>
{
    private int[]? _firstGesture;
    private bool _isCooldownActive;

    public static readonly StyledProperty<bool> AuthorizeFailedProperty =
        AvaloniaProperty.Register<GesturePasswordAuthorizeProvider, bool>(nameof(AuthorizeFailed));

    public bool AuthorizeFailed
    {
        get => GetValue(AuthorizeFailedProperty);
        set => SetValue(AuthorizeFailedProperty, value);
    }

    public static readonly StyledProperty<bool> ConfirmFailedProperty =
        AvaloniaProperty.Register<GesturePasswordAuthorizeProvider, bool>(nameof(ConfirmFailed));

    public bool ConfirmFailed
    {
        get => GetValue(ConfirmFailedProperty);
        set => SetValue(ConfirmFailedProperty, value);
    }

    public static readonly StyledProperty<bool> TooShortErrorProperty =
        AvaloniaProperty.Register<GesturePasswordAuthorizeProvider, bool>(nameof(TooShortError));

    public bool TooShortError
    {
        get => GetValue(TooShortErrorProperty);
        set => SetValue(TooShortErrorProperty, value);
    }

    public static readonly StyledProperty<bool> NeedConfirmErrorProperty =
        AvaloniaProperty.Register<GesturePasswordAuthorizeProvider, bool>(nameof(NeedConfirmError));

    public bool NeedConfirmError
    {
        get => GetValue(NeedConfirmErrorProperty);
        set => SetValue(NeedConfirmErrorProperty, value);
    }

    public static readonly StyledProperty<bool> ProtectGestureProperty =
        AvaloniaProperty.Register<GesturePasswordAuthorizeProvider, bool>(nameof(ProtectGesture));

    public bool ProtectGesture
    {
        get => GetValue(ProtectGestureProperty);
        set => SetValue(ProtectGestureProperty, value);
    }

    public GesturePasswordAuthorizeProvider()
    {
        InitializeComponent();
    }

    private void GesturePasswordAuthorizeProvider_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Settings.GestureHash) && IsEditingMode)
        {
            ProtectGesture = true;
        }
    }

    private void GestureGrid_OnGestureCompleted(object? sender, int[] path)
    {
        TooShortError = false;
        NeedConfirmError = false;
        if (IsEditingMode)
        {
            HandleEditingGesture(path);
        }
        else
        {
            HandleVerifyGesture(path);
        }
    }

    private void GestureGrid_OnGestureTooShort(object? sender, EventArgs e)
    {
        TooShortError = true;
        DispatcherTimer.RunOnce(() =>
        {
            TooShortError = false;
        }, TimeSpan.FromSeconds(1.5));
    }

    private void HandleEditingGesture(int[] path)
    {
        if (_firstGesture == null)
        {
            _firstGesture = path;
            InstructionText.Text = "请再次绘制手势以确认";
            GestureGrid.Reset();
        }
        else
        {
            if (PathsMatch(_firstGesture, path))
            {
                SaveGesture(_firstGesture);
                ProtectGesture = true;
                ConfirmFailed = false;
                NeedConfirmError = false;
                InstructionText.Text = "请绘制手势密码";
            }
            else
            {
                ConfirmFailed = true;
                _firstGesture = null;
                InstructionText.Text = "请绘制手势密码";
                DispatcherTimer.RunOnce(() =>
                {
                    ConfirmFailed = false;
                    GestureGrid.Reset();
                }, TimeSpan.FromSeconds(1.5));
            }
        }
    }

    private void HandleVerifyGesture(int[] path)
    {
        AuthorizeFailed = false;
        if (VerifyGesture(path))
        {
            CompleteAuthorize();
        }
        else
        {
            if (_isCooldownActive) return;
            _isCooldownActive = true;
            AuthorizeFailed = true;
            IsEnabled = false;
            DispatcherTimer.RunOnce(() =>
            {
                IsEnabled = true;
                _isCooldownActive = false;
                GestureGrid.Reset();
            }, TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(1000, 3000)));
        }
    }

    private static bool PathsMatch(int[] a, int[] b)
    {
        return a.Length == b.Length && a.SequenceEqual(b);
    }

    private void SaveGesture(int[] path)
    {
        var pathString = string.Join(",", path);
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var pathBytes = Encoding.UTF8.GetBytes(pathString).ToList();
        pathBytes.AddRange(saltBytes);
        var hash = SHA256.HashData(pathBytes.ToArray());
        Settings.GestureSalt = saltBytes;
        Settings.GestureHash = Convert.ToBase64String(hash);
    }

    private bool VerifyGesture(int[] path)
    {
        if (string.IsNullOrEmpty(Settings.GestureHash)) return false;

        var pathString = string.Join(",", path);
        var saltBytes = Settings.GestureSalt;
        var pathBytes = Encoding.UTF8.GetBytes(pathString).ToList();
        pathBytes.AddRange(saltBytes);
        var hash = SHA256.HashData(pathBytes.ToArray());
        var expectedHash = Convert.FromBase64String(Settings.GestureHash);
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }

    private void ButtonChangeGesture_OnClick(object sender, RoutedEventArgs e)
    {
        ProtectGesture = false;
        _firstGesture = null;
        ConfirmFailed = false;
        NeedConfirmError = false;
        InstructionText.Text = "请绘制手势密码";
        GestureGrid.Reset();
    }

    public override bool ValidateAuthorizeSettings()
    {
        if (ProtectGesture)
        {
            return true;
        }

        if (_firstGesture != null)
        {
            NeedConfirmError = true;
            return false;
        }

        TooShortError = true;
        return false;
    }
}
