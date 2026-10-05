using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    private bool _isBusy;
    private IDisposable? _confirmResetTimer;
    private IDisposable? _cooldownTimer;
    private IDisposable? _tooShortTimer;

    private sealed class PendingGestureState
    {
        public int[]? FirstGesture;
    }

    // 实例可能被复用于不同 CredentialItem 的设置，待确认草稿按设置对象隔离存放，行删则随设置回收。
    private readonly ConditionalWeakTable<GesturePasswordAuthorizeSettings, PendingGestureState> _pendingBySettings = new();

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
        GestureGrid.AddHandler(InputElement.PointerPressedEvent, GestureGrid_OnPointerPressed, RoutingStrategies.Bubble, true);
    }

    private void GestureGrid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        CancelConfirmResetTimer();
        ConfirmFailed = false;
        CancelTooShortTimer();
        TooShortError = false;
        if (!e.Handled) GestureGrid.Reset();
    }

    private void CancelConfirmResetTimer()
    {
        _confirmResetTimer?.Dispose();
        _confirmResetTimer = null;
    }

    private void CancelTooShortTimer()
    {
        _tooShortTimer?.Dispose();
        _tooShortTimer = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        // 搬家时不清 _firstGesture：草稿已在 OnSettingsChanged 中按旧设置存好，
        // 这里只停掉进行中的 timers，避免回调打到已搬走的实例状态上。
        CancelConfirmResetTimer();
        CancelTooShortTimer();
        _cooldownTimer?.Dispose();
        _cooldownTimer = null;
        _isCooldownActive = false;
        _isBusy = false;
        IsEnabled = true;
    }

    protected override void OnSettingsChanged(object? previousSettings)
    {
        var current = SettingsInternal as GesturePasswordAuthorizeSettings;
        if (previousSettings is GesturePasswordAuthorizeSettings previous
            && current != null
            && !ReferenceEquals(previous, current))
        {
            _pendingBySettings.GetOrCreateValue(previous).FirstGesture = _firstGesture;
        }
        else if (current != null)
        {
            // 同设置重取或首次附加：活状态即归属当前设置，先存后取，结果不变。
            _pendingBySettings.GetOrCreateValue(current).FirstGesture = _firstGesture;
        }
        RestoreForCurrentSettings();
    }

    private void GesturePasswordAuthorizeProvider_OnLoaded(object sender, RoutedEventArgs e)
    {
        // 首挂载时 GetInstance 已做过 stash/restore，这里重复调一次是幂等的兜底。
        RestoreForCurrentSettings();
    }

    private void ClearPendingForCurrentSettings()
    {
        if (SettingsInternal is GesturePasswordAuthorizeSettings current)
        {
            _pendingBySettings.Remove(current);
        }
    }

    private void RestoreForCurrentSettings()
    {
        if (SettingsInternal is not GesturePasswordAuthorizeSettings current)
        {
            return;
        }
        _firstGesture = _pendingBySettings.TryGetValue(current, out var state) ? state.FirstGesture : null;
        CancelConfirmResetTimer();
        CancelTooShortTimer();
        _cooldownTimer?.Dispose();
        _cooldownTimer = null;
        _isCooldownActive = false;
        _isBusy = false;
        IsEnabled = true;
        AuthorizeFailed = false;
        ConfirmFailed = false;
        TooShortError = false;
        NeedConfirmError = false;
        ProtectGesture = !string.IsNullOrEmpty(current.GestureHash) && IsEditingMode;
        InstructionText.Text = _firstGesture != null ? "请再次绘制手势以确认" : "请绘制手势密码";
        GestureGrid.Reset();
    }

    private async void GestureGrid_OnGestureCompleted(object? sender, int[] path)
    {
        TooShortError = false;
        NeedConfirmError = false;
        if (IsEditingMode)
        {
            await HandleEditingGestureAsync(path);
        }
        else
        {
            await HandleVerifyGestureAsync(path);
        }
    }

    private void GestureGrid_OnGestureTooShort(object? sender, EventArgs e)
    {
        CancelTooShortTimer();
        TooShortError = true;
        _tooShortTimer = DispatcherTimer.RunOnce(() =>
        {
            _tooShortTimer = null;
            TooShortError = false;
        }, TimeSpan.FromSeconds(1.5));
    }

    private async Task HandleEditingGestureAsync(int[] path)
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
                CancelConfirmResetTimer();
                _isBusy = true;
                IsEnabled = false;
                try
                {
                    var first = _firstGesture;
                    await Task.Run(() => SaveGesture(first));
                }
                finally
                {
                    _isBusy = false;
                    IsEnabled = true;
                }
                _firstGesture = null;
                ClearPendingForCurrentSettings();
                ProtectGesture = true;
                ConfirmFailed = false;
                NeedConfirmError = false;
                InstructionText.Text = "请绘制手势密码";
            }
            else
            {
                ConfirmFailed = true;
                _firstGesture = null;
                ClearPendingForCurrentSettings();
                InstructionText.Text = "请绘制手势密码";
                _confirmResetTimer = DispatcherTimer.RunOnce(() =>
                {
                    _confirmResetTimer = null;
                    ConfirmFailed = false;
                    GestureGrid.Reset();
                }, TimeSpan.FromSeconds(1.5));
            }
        }
    }

    private async Task HandleVerifyGestureAsync(int[] path)
    {
        if (_isCooldownActive || _isBusy)
        {
            return;
        }
        AuthorizeFailed = false;
        _isBusy = true;
        IsEnabled = false;
        bool matched;
        try
        {
            matched = await Task.Run(() => VerifyGesture(path));
        }
        finally
        {
            _isBusy = false;
        }
        if (matched)
        {
            IsEnabled = true;
            CompleteAuthorize();
        }
        else
        {
            _isCooldownActive = true;
            AuthorizeFailed = true;
            _cooldownTimer?.Dispose();
            _cooldownTimer = DispatcherTimer.RunOnce(() =>
            {
                _cooldownTimer = null;
                IsEnabled = true;
                _isCooldownActive = false;
                AuthorizeFailed = false;
                GestureGrid.Reset();
            }, TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(1000, 3000)));
        }
    }

    private static bool PathsMatch(int[] a, int[] b)
    {
        return a.Length == b.Length && a.SequenceEqual(b);
    }

    private const int HashIterations = 600_000;

    private void SaveGesture(int[] path)
    {
        var pathString = string.Join(",", path);
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pathString),
            saltBytes,
            HashIterations,
            HashAlgorithmName.SHA256,
            32);
        Settings.GestureSalt = saltBytes;
        Settings.GestureHash = Convert.ToBase64String(hash);
    }

    private bool VerifyGesture(int[] path)
    {
        if (string.IsNullOrEmpty(Settings.GestureHash)) return false;
        if (Settings.GestureSalt is not { Length: > 0 }) return false;

        try
        {
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(string.Join(",", path)),
                Settings.GestureSalt,
                HashIterations,
                HashAlgorithmName.SHA256,
                32);
            var expectedHash = Convert.FromBase64String(Settings.GestureHash);
            return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void ButtonChangeGesture_OnClick(object sender, RoutedEventArgs e)
    {
        CancelConfirmResetTimer();
        CancelTooShortTimer();
        ProtectGesture = false;
        _firstGesture = null;
        ClearPendingForCurrentSettings();
        ConfirmFailed = false;
        NeedConfirmError = false;
        TooShortError = false;
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
