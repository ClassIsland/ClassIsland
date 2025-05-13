using System.ComponentModel.DataAnnotations;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Shared.Models.Notification;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 代表一个提醒请求。
/// </summary>
public class NotificationRequest : ObservableRecipient
{
    private CancellationTokenSource _cancellationTokenSource = new();
    private NotificationSettings _requestNotificationSettings = new();
    private CancellationTokenSource _completedTokenSource = new();
    private NotificationContent? _overlayContent;
    private NotificationContent _maskContent = NotificationContent.Empty;

    /// <summary>
    /// 初始化一个 <see cref="NotificationRequest"/> 实例。
    /// </summary>
    public NotificationRequest()
    {
        CancellationTokenSource.Token.Register(() =>
        {
            Canceled?.Invoke(this, EventArgs.Empty);
        });
        CompletedTokenSource.Token.Register(() =>
        {
            Completed?.Invoke(this, EventArgs.Empty);
        });
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
    /// 提醒正文内容。如果为 null，则不显示正文。
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
    /// 代表提醒取消的取消令牌源。
    /// </summary>
    internal CancellationTokenSource CancellationTokenSource
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
    internal CancellationTokenSource CompletedTokenSource
    {
        get => _completedTokenSource;
        set
        {
            if (Equals(value, _completedTokenSource)) return;
            _completedTokenSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 代表提醒被取消的取消令牌。
    /// </summary>
    public CancellationToken CancellationToken => CancellationTokenSource.Token;

    /// <summary>
    /// 代表提醒显示完成的取消令牌。
    /// </summary>
    public CancellationToken CompletedToken => CompletedTokenSource.Token;

    /// <summary>
    /// 发送提醒的提醒渠道 ID
    /// </summary>
    public Guid ChannelId { get; set; }

    internal NotificationProviderRegisterInfo? NotificationSource { get; set; } = null;

    internal Guid NotificationSourceGuid { get; set; } = Guid.Empty;

    internal NotificationSettings ProviderSettings { get; set; } = new NotificationSettings();

    internal NotificationSettings? ChannelSettings { get; set; }

    internal bool IsPriorityOverride { get; set; } = false;

    internal int PriorityOverride { get; set; } = -1;

    internal CancellationTokenSource? RootCancellationTokenSource { get; set; }
    internal CancellationTokenSource? RootCompletedTokenSource { get; set; }

    /// <summary>
    /// 取消当前提醒。
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource.Cancel();
    }

    /// <summary>
    /// 当当前提醒被取消时触发。
    /// </summary>
    public event EventHandler? Canceled;

    /// <summary>
    /// 当当前提醒显示完成时触发。
    /// </summary>
    public event EventHandler? Completed;


#pragma warning disable CS0618 // 类型或成员已过时
    internal static NotificationRequest ConvertFromOldNotificationRequest(ClassIsland.Shared.Models.Notification.NotificationRequest oldRequest)
    {
        var newRequest = new NotificationRequest()
        {
            MaskContent = new NotificationContent()
            {
                Content = oldRequest.MaskContent,
                SpeechContent = oldRequest.MaskSpeechContent,
                Duration = oldRequest.MaskDuration,
                EndTime = oldRequest.TargetMaskEndTime,
                IsSpeechEnabled = oldRequest.IsSpeechEnabled
            },
            OverlayContent = oldRequest.OverlayContent != null ? new NotificationContent()
            {
                Content = oldRequest.OverlayContent,
                SpeechContent = oldRequest.OverlaySpeechContent,
                Duration = oldRequest.OverlayDuration,
                EndTime = oldRequest.TargetOverlayEndTime,
                IsSpeechEnabled = oldRequest.IsSpeechEnabled
            } : null,
            IsPriorityOverride = oldRequest.IsPriorityOverride,
            RequestNotificationSettings = oldRequest.RequestNotificationSettings,
            CancellationTokenSource = oldRequest.CancellationTokenSource,
            CompletedTokenSource = oldRequest.CompletedTokenSource
        };
        return newRequest;
    }
#pragma warning restore CS0618 // 类型或成员已过时
}