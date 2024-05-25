using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;


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