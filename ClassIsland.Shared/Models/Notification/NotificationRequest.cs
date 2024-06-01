using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Notification;

/// <summary>
/// 提醒请求。
/// </summary>
public class NotificationRequest : ObservableRecipient
{
    private object? _overlayContent;
    private object _maskContent = new();
    private TimeSpan _overlayDuration = TimeSpan.FromSeconds(5);
    private TimeSpan _maskDuration = TimeSpan.FromSeconds(5);
    private DateTime? _targetOverlayEndTime;
    private DateTime? _targetMaskEndTime;
    private CancellationTokenSource _cancellationTokenSource = new();
    private string _overlaySpeechContent = "";
    private string _maskSpeechContent = "";
    private bool _isSpeechEnabled = true;
    private NotificationSettings _requestNotificationSettings = new();
    private CancellationTokenSource _completedTokenSource = new();

    /// <summary>
    /// 提醒时要显示的内容
    /// </summary>
    public object? OverlayContent
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
    /// 提醒时进入横幅要显示的内容
    /// </summary>
    public object MaskContent
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
    /// 提醒内容显示时长。当<see cref="TargetOverlayEndTime"/>不为null时，此项不起作用。
    /// </summary>
    public TimeSpan OverlayDuration
    {
        get => _overlayDuration;
        set
        {
            if (value.Equals(_overlayDuration)) return;
            _overlayDuration = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 提醒进入横幅时长。当<see cref="TargetMaskEndTime"/>不为null时，此项不起作用。
    /// </summary>
    public TimeSpan MaskDuration
    {
        get => _maskDuration;
        set
        {
            if (value.Equals(_maskDuration)) return;
            _maskDuration = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 目标提醒结束时间。如果此项不为null，那么在显示此提醒时，自动将<see cref="OverlayDuration"/>设置为距离此值的时长。
    /// </summary>
    public DateTime? TargetOverlayEndTime
    {
        get => _targetOverlayEndTime;
        set
        {
            if (Nullable.Equals(value, _targetOverlayEndTime)) return;
            _targetOverlayEndTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 目标进入横幅结束时间。如果此项不为null，那么在显示此提醒时，自动将<see cref="MaskDuration"/>设置为距离此值的时长。
    /// </summary>
    public DateTime? TargetMaskEndTime
    {
        get => _targetMaskEndTime;
        set
        {
            if (Nullable.Equals(value, _targetMaskEndTime)) return;
            _targetMaskEndTime = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 代表提醒取消的取消令牌源。
    /// </summary>
    public CancellationTokenSource CancellationTokenSource
    {
        get => _cancellationTokenSource;
        set
        {
            if (Equals(value, _cancellationTokenSource)) return;
            _cancellationTokenSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 提醒内容的朗读内容。当显示提醒内容时，如果启用了朗读，这里的字符串将被大声读出。
    /// </summary>
    public string OverlaySpeechContent
    {
        get => _overlaySpeechContent;
        set
        {
            if (value == _overlaySpeechContent) return;
            _overlaySpeechContent = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 提醒进入横幅的朗读内容。当显示进入横幅时，如果启用了朗读，这里的字符串将被大声读出。
    /// </summary>
    public string MaskSpeechContent
    {
        get => _maskSpeechContent;
        set
        {
            if (value == _maskSpeechContent) return;
            _maskSpeechContent = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 此次通知是否启用朗读。
    /// </summary>
    public bool IsSpeechEnabled
    {
        get => _isSpeechEnabled;
        set
        {
            if (value == _isSpeechEnabled) return;
            _isSpeechEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 针对此次提醒的特殊设置。如果要使此设置生效，还要将<see cref="NotificationSettings.IsSettingsEnabled"/>设置为true。
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
    /// 代表提醒显示完毕的取消令牌源。
    /// </summary>
    public CancellationTokenSource CompletedTokenSource
    {
        get => _completedTokenSource;
        set
        {
            if (Equals(value, _completedTokenSource)) return;
            _completedTokenSource = value;
            OnPropertyChanged();
        }
    }


    internal NotificationProviderRegisterInfo? NotificationSource { get; set; } = null;

    internal Guid NotificationSourceGuid { get; set; } = Guid.Empty;

    internal NotificationSettings ProviderSettings { get; set; } = new NotificationSettings();

    internal bool IsPriorityOverride { get; set; } = false;

    internal int PriorityOverride { get; set; } = -1;
}