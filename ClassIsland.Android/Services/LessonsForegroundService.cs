using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Models;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Android.Services;

[Service(Exported = false,
    ForegroundServiceType = ForegroundService.TypeSpecialUse)]
[Property(
    "android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE",
    Value = "Keeps the active timetable and current lesson progress available while ClassIsland runs in the background.")]
public class LessonsForegroundService : Service
{
    private const string NotificationChannelId = "lessons_live_status_v2";
    private const int NotificationId = 1_328_0;
    private const int ContentIntentRequestCode = 1_328_1;
    private const int DeleteIntentRequestCode = 1_328_2;
    internal const string DismissedIntervalIntentExtra = "dismissed_interval";

    private static string? s_dismissedIntervalKey;

    private NotificationManager? _notificationManager;
    private bool _isForegroundStarted;
    private bool _isSubscribedToAppStopping;
    private bool _isWaitingForAppStart;
    private bool _isWorkStarted;
    private NotificationSnapshot? _lastPostedSnapshot;
    private AndroidOverlayService? _overlayService;
    private readonly RealtimeNotificationContentBuilder _contentBuilder = new();
    private (string IconExpression, string ColorHex, string? ProgressEndIcon, string? ProgressEndColorHex)? _requestedIconKey;
    private CancellationTokenSource? _iconRenderingCancellation;
    private SubjectNotificationIcons? _subjectIcons;

    private SettingsService? SettingsService { get; set; }

    private ILessonsService? LessonsService { get; set; }
    private IExactTimeService? ExactTimeService { get; set; }

    private IProfileService? ProfileService { get; set; }

    public override void OnCreate()
    {
        base.OnCreate();

        _notificationManager =
            GetSystemService(NotificationService) as NotificationManager;

        CreateNotificationChannel();
    }

    public override StartCommandResult OnStartCommand(
        Intent? intent,
        StartCommandFlags flags,
        int startId)
    {
        if (!_isForegroundStarted)
        {
            var notification = CreateNotification(NotificationSnapshot.Loading);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
#pragma warning disable CA1416
                StartForeground(
                    NotificationId,
                    notification,
                    ForegroundService.TypeSpecialUse);
#pragma warning restore CA1416
            }
            else
            {
                StartForeground(NotificationId, notification);
            }

            _isForegroundStarted = true;
        }

        if (!_isWorkStarted && !_isWaitingForAppStart)
        {
            if (AppBase.CurrentLifetime < ApplicationLifetime.Running)
            {
                _isWaitingForAppStart = true;
                AppBase.Current.AppStarted += CurrentOnAppStarted;
            }
            else if (AppBase.CurrentLifetime == ApplicationLifetime.Running)
            {
                StartWork();
            }
        }

        // The timetable services are initialized by MainActivity. A headless sticky
        // restart would otherwise wait for AppStarted forever and leave a stale
        // "loading" notification behind.
        return StartCommandResult.NotSticky;
    }

    private void CurrentOnAppStarted(object? sender, EventArgs e)
    {
        AppBase.Current.AppStarted -= CurrentOnAppStarted;
        _isWaitingForAppStart = false;
        StartWork();
    }

    private void StartWork()
    {
        if (_isWorkStarted)
        {
            return;
        }

        LessonsService = IAppHost.GetService<ILessonsService>();
        ExactTimeService = IAppHost.GetService<IExactTimeService>();
        SettingsService = IAppHost.GetService<SettingsService>();
        ProfileService = IAppHost.GetService<IProfileService>();
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        AppBase.Current.AppStopping += CurrentOnAppStopping;
        _isSubscribedToAppStopping = true;
        _isWorkStarted = true;
        RefreshNotification();
        Dispatcher.UIThread.Post(() =>
        {
            if (_isWorkStarted && _isSubscribedToAppStopping)
            {
                _overlayService ??= new AndroidOverlayService(this);
            }
        });
    }

    private void CurrentOnAppStopping(object? sender, EventArgs e)
    {
        StopOverlay();
        if (_isSubscribedToAppStopping)
        {
            AppBase.Current.AppStopping -= CurrentOnAppStopping;
            _isSubscribedToAppStopping = false;
        }

        StopNotificationIcons();
        StopForeground(StopForegroundFlags.Remove);
        StopSelf();
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        RefreshNotification();
    }

    private void RefreshNotification()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RefreshNotification);
            return;
        }

        if (!_isWorkStarted || !_isSubscribedToAppStopping ||
            LessonsService == null || ExactTimeService == null)
        {
            return;
        }

        var snapshot = CreateSnapshot();
        if (IsIntervalDismissed(snapshot.IntervalKey))
        {
            return;
        }

        UpdateSubjectIcons(snapshot);
        if (snapshot == _lastPostedSnapshot)
        {
            return;
        }

        using var notification = CreateNotification(snapshot);
        _notificationManager?.Notify(NotificationId, notification);
        _lastPostedSnapshot = snapshot;
    }

    private void UpdateSubjectIcons(NotificationSnapshot snapshot)
    {
        (string IconExpression, string ColorHex, string? ProgressEndIcon, string? ProgressEndColorHex)? key = snapshot.HasProgress
            ? (snapshot.SubjectIcon ?? Subject.Fallback.Icon, snapshot.SubjectColorHex ?? Subject.Fallback.ColorHex,
                snapshot.ProgressEndIcon, snapshot.ProgressEndColorHex)
            : null;
        if (key == _requestedIconKey)
        {
            return;
        }

        ClearNotificationIcons();
        _requestedIconKey = key;
        if (key is not { } iconKey)
        {
            return;
        }

        _iconRenderingCancellation = new CancellationTokenSource();
        _ = RenderSubjectIconsAsync(iconKey, _iconRenderingCancellation.Token);
    }

    private async Task RenderSubjectIconsAsync(
        (string IconExpression, string ColorHex, string? ProgressEndIcon, string? ProgressEndColorHex) key,
        CancellationToken cancellationToken)
    {
        SubjectNotificationIcons? icons = null;
        try
        {
            icons = await SubjectNotificationIcons.RenderAsync(
                key.IconExpression, key.ColorHex, key.ProgressEndIcon, key.ProgressEndColorHex, cancellationToken);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested || !_isWorkStarted ||
                    !_isSubscribedToAppStopping || _requestedIconKey != key)
                {
                    return;
                }

                _subjectIcons = icons;
                icons = null;
                // Rendered icons replace the placeholders even when text and progress are unchanged.
                _lastPostedSnapshot = null;
                RefreshNotification();
            }, DispatcherPriority.Normal, cancellationToken);
        }
        catch (System.OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            IAppHost.GetService<ILogger<LessonsForegroundService>>()
                .LogWarning(exception, "无法渲染科目通知图标，将使用默认图标");
        }
        finally
        {
            icons?.Dispose();
        }
    }

    private void StopNotificationIcons()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ClearNotificationIcons();
        }
        else
        {
            Dispatcher.UIThread.Post(ClearNotificationIcons);
        }
    }

    private void ClearNotificationIcons()
    {
        _iconRenderingCancellation?.Cancel();
        _iconRenderingCancellation?.Dispose();
        _iconRenderingCancellation = null;
        _requestedIconKey = null;
        _subjectIcons?.Dispose();
        _subjectIcons = null;
    }

    private NotificationSnapshot CreateSnapshot()
    {
        var now = ExactTimeService!.GetCurrentLocalDateTime();
        if (LessonsService!.CurrentState == TimeState.OnClass)
        {
            var item = LessonsService.CurrentTimeLayoutItem;
            if (!ReferenceEquals(item, TimeLayoutItem.Empty) && item.EndTime > item.StartTime)
            {
                var settings = GetEffectiveSettings(isUpcoming: false);
                return CreateCourseSnapshot(now, item, LessonsService.CurrentSubject,
                    LessonsService.CurrentClassInfo, settings, isUpcoming: false);
            }
        }
        else if (LessonsService.CurrentState is TimeState.Breaking or TimeState.None)
        {
            var item = LessonsService.NextClassTimeLayoutItem;
            var settings = GetEffectiveSettings(isUpcoming: true);
            var remaining = item.StartTime - now.TimeOfDay;
            if (!ReferenceEquals(item, TimeLayoutItem.Empty) &&
                item.EndTime > item.StartTime && remaining > TimeSpan.Zero &&
                remaining <= TimeSpan.FromMinutes(settings.UpcomingClassRealtimeNotificationLeadMinutes))
            {
                return CreateCourseSnapshot(now, item, LessonsService.NextClassSubject,
                    LessonsService.NextClassInfo, settings, isUpcoming: true);
            }

            if (LessonsService.CurrentState == TimeState.Breaking)
            {
                var breakItem = LessonsService.CurrentTimeLayoutItem;
                if (!ReferenceEquals(breakItem, TimeLayoutItem.Empty) && breakItem.EndTime > breakItem.StartTime)
                {
                    return CreateCourseSnapshot(now, breakItem, LessonsService.CurrentSubject,
                        null, GetEffectiveSettings(isUpcoming: false), isUpcoming: false);
                }
            }
        }

        return new NotificationSnapshot(
            LessonsService.CurrentState,
            $"background:{now:yyyyMMdd}:{LessonsService.CurrentState}",
            "ClassIsland",
            "ClassIsland 正在后台运行",
            "ClassIsland",
            string.Empty);
    }

    private IRealtimeNotificationSettings GetEffectiveSettings(bool isUpcoming)
    {
        var attached = IAttachedSettingsHostService.GetAttachedSettingsByPriority<RealtimeNotificationAttachedSettings>(
            Guid.Parse(RealtimeNotificationAttachedSettings.SettingsId),
            isUpcoming ? LessonsService!.NextClassSubject : LessonsService!.CurrentSubject,
            isUpcoming ? LessonsService.NextClassTimeLayoutItem : LessonsService.CurrentTimeLayoutItem,
            LessonsService.CurrentClassPlan,
            LessonsService.CurrentClassPlan?.TimeLayout);
        return (IRealtimeNotificationSettings?)attached ?? SettingsService!.Settings;
    }

    private NotificationSnapshot CreateCourseSnapshot(
        DateTime now,
        TimeLayoutItem item,
        Subject? subject,
        ClassInfo? classInfo,
        IRealtimeNotificationSettings settings,
        bool isUpcoming)
    {
        var state = isUpcoming ? TimeState.None : LessonsService!.CurrentState;
        var intervalKey = CreateIntervalKey(state, item, now);
        var targetTime = isUpcoming ? item.StartTime : item.EndTime;
        var duration = isUpcoming
            ? TimeSpan.FromMinutes(settings.UpcomingClassRealtimeNotificationLeadMinutes)
            : item.EndTime - item.StartTime;
        var totalSeconds = (int)Math.Clamp(Math.Ceiling(duration.TotalSeconds), 1, int.MaxValue);
        var remainingSeconds = (int)Math.Clamp(
            Math.Ceiling((targetTime - now.TimeOfDay).TotalSeconds), 0, totalSeconds);
        var progressSeconds = totalSeconds - remainingSeconds;
        var progressPercent = (int)Math.Round(
            progressSeconds * 100.0 / totalSeconds,
            MidpointRounding.AwayFromZero);
        var content = _contentBuilder.Build(settings, subject, classInfo, item,
            RealtimeNotificationContentBuilder.FormatDuration(remainingSeconds),
            isUpcoming, intervalKey, System.Environment.TickCount64);
        var nextSubject = RealtimeNotificationContentBuilder.GetNextClassSubject(
            LessonsService!.CurrentClassPlan, ProfileService!.Profile.Subjects,
            LessonsService.CurrentTimeLayoutItem, now.TimeOfDay);

        return new NotificationSnapshot(
            state,
            intervalKey,
            content.ExpandedTitle,
            content.ExpandedContent,
            content.Summary,
            content.CriticalText,
            100,
            progressPercent,
            progressPercent,
            isUpcoming
                ? settings.IsUpcomingClassRealtimeNotificationEnabled
                : state == TimeState.OnClass && settings.IsCurrentClassRealtimeNotificationEnabled,
            subject?.Icon ?? Subject.Fallback.Icon,
            subject?.ColorHex ?? Subject.Fallback.ColorHex,
            nextSubject?.Icon,
            nextSubject?.ColorHex);
    }

    public override void OnDestroy()
    {
        StopOverlay();
        if (_isWaitingForAppStart)
        {
            AppBase.Current.AppStarted -= CurrentOnAppStarted;
            _isWaitingForAppStart = false;
        }

        if (_isWorkStarted && LessonsService != null)
        {
            LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
        }

        if (_isSubscribedToAppStopping)
        {
            AppBase.Current.AppStopping -= CurrentOnAppStopping;
            _isSubscribedToAppStopping = false;
        }

        _isForegroundStarted = false;
        _isWorkStarted = false;
        _lastPostedSnapshot = null;
        LessonsService = null;
        ExactTimeService = null;
        SettingsService = null;
        ProfileService = null;
        StopNotificationIcons();

        base.OnDestroy();
    }

    private void StopOverlay()
    {
        var overlay = _overlayService;
        _overlayService = null;
        if (Dispatcher.UIThread.CheckAccess())
        {
            overlay?.Dispose();
        }
        else
        {
            Dispatcher.UIThread.Post(() => overlay?.Dispose());
        }
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26) || _notificationManager == null)
        {
            return;
        }

        var channel = new NotificationChannel(
            NotificationChannelId,
            "实时课程状态",
            NotificationImportance.High)
        {
            Description = "显示当前课程、课间与时间点进度"
        };
        channel.SetSound(null, null);
        channel.EnableVibration(false);

        _notificationManager.CreateNotificationChannel(channel);
    }

    private Notification CreateNotification(NotificationSnapshot snapshot)
    {
        using var builder = new NotificationCompat.Builder(this, NotificationChannelId);
        builder.SetSmallIcon(ResourceConstant.Drawable.ic_logo_monochrome_notification);
        if (snapshot.HasProgress)
        {
            builder.SetColor(SubjectNotificationIcons.GetThemeColor(snapshot.SubjectColorHex));
            if (_subjectIcons != null)
            {
                using var icon = IconCompat.CreateWithBitmap(_subjectIcons.Monochrome);
                builder.SetSmallIcon(icon);
                builder.SetLargeIcon(_subjectIcons.Color);
            }
        }
        builder.SetContentTitle(snapshot.HasProgress ? snapshot.Summary : snapshot.Title);
        builder.SetContentText(snapshot.HasProgress
            ? $"{snapshot.Title} {snapshot.Content}"
            : snapshot.Content);
        builder.SetOngoing(true);
        builder.SetOnlyAlertOnce(true);
        builder.SetSilent(true);
        builder.SetPriority(NotificationCompat.PriorityHigh);
        builder.SetCategory(snapshot.HasProgress
            ? NotificationCompat.CategoryProgress
            : NotificationCompat.CategoryService);
        builder.SetColorized(false);
        builder.SetContentIntent(CreateContentIntent());
        builder.SetDeleteIntent(CreateDeleteIntent(snapshot.IntervalKey));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Baklava && snapshot.HasProgress)
        {
            ConfigureLiveUpdate(builder, snapshot);
        }
        else if (Build.VERSION.SdkInt < BuildVersionCodes.Baklava && snapshot.HasProgress)
        {
            ConfigureCustomNotification(builder, snapshot);
        }

        return builder.Build()!;
    }

    private void ConfigureLiveUpdate(
        NotificationCompat.Builder builder,
        NotificationSnapshot snapshot)
    {
        using var style = new NotificationCompat.ProgressStyle();
        var segment = new NotificationCompat.ProgressStyle.Segment(snapshot.ProgressMax);
        segment.SetColor(SubjectNotificationIcons.GetThemeColor(snapshot.SubjectColorHex));
        style.SetProgressSegments(
        [
            segment
        ]);
        style.SetProgress(snapshot.Progress);
        if (_subjectIcons?.ProgressEnd is { } progressEnd)
        {
            using var icon = IconCompat.CreateWithBitmap(progressEnd);
            style.SetProgressEndIcon(icon);
        }

        builder.SetStyle(style);
        // Disabling promotion keeps the foreground service and course progress notification active.
        builder.SetRequestPromotedOngoing(snapshot.RequestPromotion);
        builder.SetShortCriticalText(snapshot.CriticalText);
        builder.SetShowWhen(false);
    }

    private void ConfigureCustomNotification(
        NotificationCompat.Builder builder,
        NotificationSnapshot snapshot)
    {
        var compactViews = new RemoteViews(
            PackageName,
            ResourceConstant.Layout.notification_lessons);
        var expandedViews = new RemoteViews(
            PackageName,
            ResourceConstant.Layout.notification_lessons_expanded);

        compactViews.SetTextViewText(
            ResourceConstant.Id.notification_title,
            snapshot.Summary);
        compactViews.SetViewVisibility(
            ResourceConstant.Id.notification_content,
            ViewStates.Gone);
        compactViews.SetViewVisibility(
            ResourceConstant.Id.notification_remaining,
            ViewStates.Gone);
        expandedViews.SetTextViewText(
            ResourceConstant.Id.notification_title,
            snapshot.Title);
        expandedViews.SetTextViewText(
            ResourceConstant.Id.notification_content,
            snapshot.Content);
        var progressEnd = _subjectIcons?.ProgressEnd;
        var iconVisibility = progressEnd != null ? ViewStates.Visible : ViewStates.Gone;
        compactViews.SetViewVisibility(ResourceConstant.Id.notification_progress_end_icon, iconVisibility);
        expandedViews.SetViewVisibility(ResourceConstant.Id.notification_progress_end_icon, iconVisibility);
        if (progressEnd != null)
        {
            compactViews.SetImageViewBitmap(ResourceConstant.Id.notification_progress_end_icon, progressEnd);
            expandedViews.SetImageViewBitmap(ResourceConstant.Id.notification_progress_end_icon, progressEnd);
        }

        var progressVisibility = snapshot.HasProgress
            ? ViewStates.Visible
            : ViewStates.Gone;
        compactViews.SetViewVisibility(
            ResourceConstant.Id.notification_progress_container,
            progressVisibility);
        expandedViews.SetViewVisibility(
            ResourceConstant.Id.notification_progress_container,
            progressVisibility);

        if (snapshot.HasProgress)
        {
            compactViews.SetProgressBar(
                ResourceConstant.Id.notification_progress,
                snapshot.ProgressMax,
                snapshot.Progress,
                false);
            expandedViews.SetTextViewText(
                ResourceConstant.Id.notification_progress_percent,
                $"{snapshot.ProgressPercent}%");
            expandedViews.SetProgressBar(
                ResourceConstant.Id.notification_progress,
                snapshot.ProgressMax,
                snapshot.Progress,
                false);
            builder.SetProgress(snapshot.ProgressMax, snapshot.Progress, false);
        }

        builder.SetCustomContentView(compactViews);
        builder.SetCustomBigContentView(expandedViews);
        builder.SetStyle(new NotificationCompat.DecoratedCustomViewStyle());
    }

    private PendingIntent CreateContentIntent()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        return PendingIntent.GetActivity(
            this,
            ContentIntentRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    private PendingIntent CreateDeleteIntent(string intervalKey)
    {
        var intent = new Intent(
            this,
            typeof(LessonsNotificationDismissReceiver));
        intent.PutExtra(DismissedIntervalIntentExtra, intervalKey);
        return PendingIntent.GetBroadcast(
            this,
            DeleteIntentRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    private bool IsIntervalDismissed(string intervalKey)
    {
        var dismissedIntervalKey = Volatile.Read(ref s_dismissedIntervalKey);
        if (dismissedIntervalKey == intervalKey)
        {
            return true;
        }

        if (dismissedIntervalKey == null)
        {
            return false;
        }

        Volatile.Write(ref s_dismissedIntervalKey, null);
        return false;
    }

    internal static void MarkIntervalDismissed(string intervalKey)
    {
        Volatile.Write(ref s_dismissedIntervalKey, intervalKey);
    }

    private static string CreateIntervalKey(
        TimeState state,
        TimeLayoutItem item,
        DateTime now) =>
        $"{now:yyyyMMdd}:{state}:{item.StartTime.Ticks}:{item.EndTime.Ticks}";

    private sealed record NotificationSnapshot(
        TimeState State,
        string IntervalKey,
        string Title,
        string Content,
        string Summary,
        string CriticalText,
        int ProgressMax = 0,
        int Progress = 0,
        int ProgressPercent = 0,
        bool RequestPromotion = false,
        string? SubjectIcon = null,
        string? SubjectColorHex = null,
        string? ProgressEndIcon = null,
        string? ProgressEndColorHex = null)
    {
        public bool HasProgress => ProgressMax > 0;

        public static NotificationSnapshot Loading { get; } = new(
            TimeState.None,
            "loading",
            "ClassIsland",
            "正在加载课程状态…",
            "正在加载课程状态…",
            string.Empty);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class LessonsNotificationDismissReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        var intervalKey = intent?.GetStringExtra(
            LessonsForegroundService.DismissedIntervalIntentExtra);
        if (string.IsNullOrEmpty(intervalKey))
        {
            return;
        }

        LessonsForegroundService.MarkIntervalDismissed(intervalKey);
    }
}
