using System.ComponentModel.DataAnnotations;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Enums.Notification;
using ClassIsland.Shared.Models.Notification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 代表一个提醒请求。
/// </summary>
public class NotificationRequest : ObservableRecipient
{
    private NotificationSettings _requestNotificationSettings = new();
    private NotificationContent? _overlayContent;
    private NotificationContent _maskContent = NotificationContent.Empty;
    private int? _targetLineNumber;

    /// <summary>
    /// 初始化一个 <see cref="NotificationRequest"/> 实例。
    /// </summary>
    public NotificationRequest()
    {
        Lifecycle = new NotificationLifecycle(this);
        // 兼容性处理
        Lifecycle.RaisePropertyChangedAction = OnPropertyChanged;
    }

    /// <summary>
    /// 通知生命周期管理
    /// </summary>
    public NotificationLifecycle Lifecycle { get; }

    /// <summary>
    /// 指定通知路由到目标行
    /// 若为 <see langword="null"/>, 则由系统自动路由
    /// </summary>
    public int? TargetLineNumber
    {
        get => _targetLineNumber;
        set
        {
            if (value == _targetLineNumber) return;
            _targetLineNumber = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 提醒遮罩内容
    /// </summary>
    [Required]
    public NotificationContent MaskContent
    {
        get => _maskContent;
        set
        {
            if (Equals(value, _maskContent)) return;
            _maskContent = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 提醒正文内容
    /// 若为 <see langword="null"/>, 则不显示正文
    /// </summary>
    public NotificationContent? OverlayContent
    {
        get => _overlayContent;
        set
        {
            if (Equals(value, _overlayContent)) return;
            _overlayContent = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 针对此次提醒的特殊设置
    /// 若要使此设置生效, 还要将<see cref="NotificationSettings.IsSettingsEnabled"/>设置为 true
    /// </summary>
    public NotificationSettings RequestNotificationSettings
    {
        get => _requestNotificationSettings;
        set
        {
            if (Equals(value, _requestNotificationSettings)) return;
            _requestNotificationSettings = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 发送提醒的提醒渠道 ID
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// 通知有效分配时间
    /// 若为 <see langword="null"/>, 则无时效限制
    /// </summary>
    public TimeSpan? ValidityDuration { get; set; }

    internal NotificationRequest? ChainedNextRequest { get; set; }

    internal NotificationRequest? ChainedHeadRequest { get; set; }

    /// <summary>
    /// 此请求所属的通知组
    /// </summary>
    internal NotificationGroup? Group { get; set; }

    /// <summary>
    /// 提醒遮罩播放会话
    /// </summary>
    public NotificationPlayingSessionInfo MaskSession { get; } = new();

    /// <summary>
    /// 提醒正文播放会话
    /// </summary>
    public NotificationPlayingSessionInfo OverlaySession { get; } = new();

    internal Guid NotificationSourceGuid { get; set; } = Guid.Empty;

    internal NotificationProviderRegisterInfo? NotificationSource { get; set; } = null;

    internal NotificationSettings ProviderSettings { get; set; } = new NotificationSettings();

    internal NotificationSettings? ChannelSettings { get; set; }

    internal bool IsPriorityOverride { get; set; } = false;

    internal int PriorityOverride { get; set; } = -1;

    internal bool NotificationSetupCompleted { get; set; }

    internal int InitialQueueIndex { get; set; } = -1;

    internal CancellationTokenSource? RootCancellationTokenSource { get; set; }
    internal CancellationTokenSource? RootCompletedTokenSource { get; set; }

    // 一堆[Obsolete]

    /// <inheritdoc cref="NotificationLifecycle.State"/>
    [Obsolete("请改用 Lifecycle.State")]
    public NotificationState State
    {
        get => Lifecycle.State;
        internal set => Lifecycle.State = value;
    }

    /// <inheritdoc cref="NotificationLifecycle.CancellationTokenSource"/>
    [Obsolete("请改用 Lifecycle.CancellationTokenSource")]
    internal CancellationTokenSource CancellationTokenSource
    {
        get => Lifecycle.CancellationTokenSource;
    }

    /// <inheritdoc cref="NotificationLifecycle.CompletedTokenSource"/>
    [Obsolete("请改用 Lifecycle.CompletedTokenSource")]
    internal CancellationTokenSource CompletedTokenSource
    {
        get => Lifecycle.CompletedTokenSource;
    }

    /// <inheritdoc cref="NotificationLifecycle.CancellationToken"/>
    [Obsolete("请改用 Lifecycle.CancellationToken")]
    public CancellationToken CancellationToken => Lifecycle.CancellationToken;

    /// <inheritdoc cref="NotificationLifecycle.CompletedToken"/>
    [Obsolete("请改用 Lifecycle.CompletedToken")]
    public CancellationToken CompletedToken => Lifecycle.CompletedToken;

    /// <inheritdoc cref="NotificationLifecycle.LeftProgress"/>
    [Obsolete("请改用 Lifecycle.LeftProgress")]
    public double LeftProgress
    {
        get => Lifecycle.LeftProgress;
        internal set => Lifecycle.LeftProgress = value;
    }

    /// <inheritdoc cref="NotificationLifecycle.Canceled"/>
    [Obsolete("请改用 Lifecycle.Canceled")]
    public event EventHandler? Canceled
    {
        add => Lifecycle.Canceled += value;
        remove => Lifecycle.Canceled -= value;
    }

    /// <inheritdoc cref="NotificationLifecycle.Completed"/>
    [Obsolete("请改用 Lifecycle.Completed")]
    public event EventHandler? Completed
    {
        add => Lifecycle.Completed += value;
        remove => Lifecycle.Completed -= value;
    }

    /// <inheritdoc cref="NotificationLifecycle.Cancel"/>
    [Obsolete("请改用 Lifecycle.Cancel()")]
    public void Cancel()
    {
        Lifecycle.Cancel();
    }

    /// <inheritdoc cref="NotificationLifecycle.Pause"/>
    [Obsolete("请改用 Lifecycle.Pause()")]
    public void Pause()
    {
        Lifecycle.Pause();
    }

    /// <inheritdoc cref="NotificationLifecycle.Resume"/>
    [Obsolete("请改用 Lifecycle.Resume()")]
    public void Resume()
    {
        Lifecycle.Resume();
    }

    /// <inheritdoc cref="NotificationLifecycle.ResetCancellationTokensForTransfer"/>
    [Obsolete("请改用 Lifecycle.ResetCancellationTokensForTransfer()")]
    internal void ResetCancellationTokensForTransfer()
    {
        Lifecycle.ResetCancellationTokensForTransfer();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"NotificationRequest{{ Mask = {MaskContent}, Overlay = {OverlayContent} }}(#{GetHashCode()})";
    }
}
