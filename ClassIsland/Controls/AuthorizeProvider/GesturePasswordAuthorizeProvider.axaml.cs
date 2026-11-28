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
    private bool _isChangingGesture;
    private bool _isCooldownActive;
    private bool _isBusy;
    private IDisposable? _confirmResetTimer;
    private IDisposable? _cooldownTimer;
    private IDisposable? _tooShortTimer;

    private sealed class PendingGestureState
    {
        public int[]? FirstGesture;
        public bool IsChangingGesture;
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
        // 搬家时不清 _firstGesture：草稿已在 SetFirstGesture 中按当前设置存好，
        // 这里只停掉进行中的 timers，避免回调打到已搬走的实例状态上。
        CancelConfirmResetTimer();
        CancelTooShortTimer();
        _cooldownTimer?.Dispose();
        _cooldownTimer = null;
        _isCooldownActive = false;
    }

    protected override void OnSettingsChanged(object? previousSettings)
    {
        var current = SettingsInternal as GesturePasswordAuthorizeSettings;
        if (previousSettings is GesturePasswordAuthorizeSettings previous
            && current != null
            && !ReferenceEquals(previous, current))
        {
            var state = _pendingBySettings.GetOrCreateValue(previous);
            state.FirstGesture = _firstGesture;
            state.IsChangingGesture = _isChangingGesture;
        }
        else if (current != null)
        {
            // 同设置重取或首次附加：活状态即归属当前设置，先存后取，结果不变。
            var state = _pendingBySettings.GetOrCreateValue(current);
            state.FirstGesture = _firstGesture;
            state.IsChangingGesture = _isChangingGesture;
        }
        RestoreForCurrentSettings();
    }

    private void GesturePasswordAuthorizeProvider_OnLoaded(object sender, RoutedEventArgs e)
    {
        // 重挂载时草稿已随每次改动同步到设置，这里恢复即可；首挂载时也是幂等的。
        RestoreForCurrentSettings();
    }

    private void SetFirstGesture(int[]? gesture)
    {
        _firstGesture = gesture;
        // 每次改动都同步到当前设置的草稿，避免控件卸载重挂时用旧草稿覆盖用户已绘制的第一次手势。
        if (SettingsInternal is GesturePasswordAuthorizeSettings current)
        {
            _pendingBySettings.GetOrCreateValue(current).FirstGesture = gesture;
        }
    }

    private void SetChangingGesture(bool isChanging)
    {
        _isChangingGesture = isChanging;
        if (SettingsInternal is GesturePasswordAuthorizeSettings current)
        {
            _pendingBySettings.GetOrCreateValue(current).IsChangingGesture = isChanging;
        }
    }

    private void RestoreForCurrentSettings()
    {
        if (SettingsInternal is not GesturePasswordAuthorizeSettings current)
        {
            return;
        }
        if (_pendingBySettings.TryGetValue(current, out var state))
        {
            _firstGesture = state.FirstGesture;
            _isChangingGesture = state.IsChangingGesture;
        }
        else
        {
            _firstGesture = null;
            _isChangingGesture = false;
        }
        CancelConfirmResetTimer();
        CancelTooShortTimer();
        _cooldownTimer?.Dispose();
        _cooldownTimer = null;
        _isCooldownActive = false;
        IsEnabled = !_isBusy;
        AuthorizeFailed = false;
        ConfirmFailed = false;
        TooShortError = false;
        NeedConfirmError = false;
        ProtectGesture = !string.IsNullOrEmpty(current.GestureHash) && IsEditingMode && !_isChangingGesture;
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
        if (_isBusy)
        {
            return;
        }

        if (_firstGesture == null)
        {
            SetFirstGesture(path);
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
                var settings = Settings;
                try
                {
                    var first = _firstGesture;
                    await Task.Run(() => SaveGesture(settings, first));
                }
                finally
                {
                    _isBusy = false;
                    IsEnabled = true;
                }
                if (ReferenceEquals(SettingsInternal, settings))
                {
                    SetFirstGesture(null);
                    SetChangingGesture(false);
                    ProtectGesture = true;
                    ConfirmFailed = false;
                    NeedConfirmError = false;
                    InstructionText.Text = "请绘制手势密码";
                }
            }
            else
            {
                ConfirmFailed = true;
                SetFirstGesture(null);
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

    private static void SaveGesture(GesturePasswordAuthorizeSettings settings, int[] path)
    {
        var pathString = string.Join(",", path);
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pathString),
            saltBytes,
            HashIterations,
            HashAlgorithmName.SHA256,
            32);
        settings.GestureSalt = saltBytes;
        settings.GestureHash = Convert.ToBase64String(hash);
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
        SetChangingGesture(true);
        ProtectGesture = false;
        SetFirstGesture(null);
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
