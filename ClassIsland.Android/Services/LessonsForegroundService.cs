using System.Globalization;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;

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

    private ILessonsService? LessonsService { get; set; }
    private IExactTimeService? ExactTimeService { get; set; }

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
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        AppBase.Current.AppStopping += CurrentOnAppStopping;
        _isSubscribedToAppStopping = true;
        _isWorkStarted = true;
        RefreshNotification();
    }

    private static string GetDeltaString(DateTime now, TimeSpan target)
    {
        var v = target - now.TimeOfDay;
        return v.TotalSeconds switch // 显示秒数
        {
            >= 3600 => $"{Math.Floor(v.TotalHours)}:{v.Minutes:00}:{v.Seconds:00}",
            >= 60 => $"{v.Minutes}:{v.Seconds:00}",
            >= 0 => $"{v.Seconds}s",
            _ => ""
        };
    }

    private void CurrentOnAppStopping(object? sender, EventArgs e)
    {
        if (_isSubscribedToAppStopping)
        {
            AppBase.Current.AppStopping -= CurrentOnAppStopping;
            _isSubscribedToAppStopping = false;
        }

        StopForeground(StopForegroundFlags.Remove);
        StopSelf();
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        RefreshNotification();
    }

    private void RefreshNotification()
    {
        if (LessonsService == null || ExactTimeService == null)
        {
            return;
        }

        var snapshot = CreateSnapshot();
        if (IsIntervalDismissed(snapshot.IntervalKey))
        {
            return;
        }

        if (snapshot == _lastPostedSnapshot)
        {
            return;
        }

        _notificationManager?.Notify(
            NotificationId,
            CreateNotification(snapshot));
        _lastPostedSnapshot = snapshot;
    }

    private NotificationSnapshot CreateSnapshot()
    {
        var now = ExactTimeService!.GetCurrentLocalDateTime();
        return LessonsService!.CurrentState switch
        {
            TimeState.OnClass => CreateProgressSnapshot(now, isClass: true),
            TimeState.Breaking when LessonsService?.NextClassTimeLayoutItem != TimeLayoutItem.Empty => CreateUpcomingClassSnapshot(now),
            TimeState.Breaking => CreateProgressSnapshot(now, isClass: false),
            TimeState.None when LessonsService?.NextClassTimeLayoutItem != TimeLayoutItem.Empty => CreateUpcomingClassSnapshot(now),
            TimeState.AfterSchool => new NotificationSnapshot(
                TimeState.AfterSchool,
                $"after-school:{now:yyyyMMdd}",
                "已放学",
                "好耶！放学了！(≧∀≦)ゞ",
                string.Empty,
                "已放学"),
            _ => new NotificationSnapshot(
                TimeState.None,
                $"none:{now:yyyyMMdd}",
                "当前无课程",
                "ClassIsland 正在后台运行",
                string.Empty,
                "")
        };
    }

    private NotificationSnapshot CreateProgressSnapshot(DateTime now, bool isClass)
    {
        var item = LessonsService!.CurrentTimeLayoutItem;
        var duration = item.EndTime - item.StartTime;
        var state = isClass ? TimeState.OnClass : TimeState.Breaking;
        var intervalKey = CreateIntervalKey(state, item, now);

        if (ReferenceEquals(item, TimeLayoutItem.Empty) || duration <= TimeSpan.Zero)
        {
            var fallbackTitle = isClass
                ? $"上课 · {GetSubjectName(LessonsService.CurrentSubject, "未命名课程")}"
                : $"课间 · {item.BreakNameText}";
            var fallbackContent = isClass
                ? LessonsService.CurrentSubject?.TeacherName
                : CreateNextClassText(includeStartTime: true);
            return new NotificationSnapshot(
                state,
                intervalKey,
                fallbackTitle,
                string.IsNullOrWhiteSpace(fallbackContent)
                    ? isClass ? "上课中" : "课间休息中"
                    : fallbackContent,
                ReferenceEquals(item, TimeLayoutItem.Empty)
                    ? string.Empty
                    : FormatTimeRange(item),
                "???");
        }

        var totalSeconds = (int)Math.Clamp(
            Math.Ceiling(duration.TotalSeconds),
            1,
            int.MaxValue);
        var progressSeconds = (int)Math.Clamp(
            Math.Floor((now.TimeOfDay - item.StartTime).TotalSeconds),
            0,
            totalSeconds);
        var remainingSeconds = totalSeconds - progressSeconds;
        var progressPercent = (int)Math.Round(
            progressSeconds * 100.0 / totalSeconds,
            MidpointRounding.AwayFromZero);
        var remainingText = FormatDuration(remainingSeconds);

        string title;
        string content;
        string shortTitle;
        if (isClass)
        {
            var subjectName = GetSubjectName(
                LessonsService.CurrentSubject,
                "未命名课程");
            var teacherName = LessonsService.CurrentSubject?.TeacherName;
            title = $"上课 · {subjectName}";
            shortTitle = $"{subjectName} -{GetDeltaString(now, item.EndTime)}";
            content = string.IsNullOrWhiteSpace(teacherName)
                ? $"剩余 {remainingText}"
                : $"{teacherName} · 剩余 {remainingText}";
        }
        else
        {
            title = $"课间 · {item.BreakNameText}";
            var nextClassText = CreateNextClassText(includeStartTime: true);
            shortTitle = $"{item.BreakNameText} -{GetDeltaString(now, item.EndTime)}";
            content = string.IsNullOrEmpty(nextClassText)
                ? $"剩余 {remainingText}"
                : $"{nextClassText} · 剩余 {remainingText}";
        }

        return new NotificationSnapshot(
            state,
            intervalKey,
            title,
            content,
            FormatTimeRange(item),
            shortTitle,
            totalSeconds,
            progressSeconds,
            progressPercent,
            remainingText);
    }

    private NotificationSnapshot CreateUpcomingClassSnapshot(DateTime now)
    {
        var nextItem = LessonsService!.NextClassTimeLayoutItem;
        var hasNextClass = !ReferenceEquals(nextItem, TimeLayoutItem.Empty) &&
                           nextItem.EndTime > nextItem.StartTime;
        var nextSubjectName = GetSubjectName(LessonsService.NextClassSubject, string.Empty);
        
        var item = LessonsService!.CurrentTimeLayoutItem == TimeLayoutItem.Empty
            ? LessonsService!.CurrentClassPlan?.TimeLayout?.Layouts
                .Reverse()
                .FirstOrDefault(i =>
                    i.TimeType == 0 &&
                    i.EndTime < now.TimeOfDay)
            : LessonsService!.CurrentTimeLayoutItem;
        var startTime = item?.StartTime ?? now.TimeOfDay;
        var duration = nextItem.StartTime - startTime;
        var totalSeconds = (int)Math.Clamp(
            Math.Ceiling(duration.TotalSeconds),
            1,
            int.MaxValue);
        var progressSeconds = (int)Math.Clamp(
            Math.Floor((now.TimeOfDay - startTime).TotalSeconds),
            0,
            totalSeconds);
        var remainingSeconds = totalSeconds - progressSeconds;
        var progressPercent = (int)Math.Round(
            progressSeconds * 100.0 / totalSeconds,
            MidpointRounding.AwayFromZero);
        var remainingText = FormatDuration(remainingSeconds);

        if (hasNextClass && !string.IsNullOrEmpty(nextSubjectName))
        {
            return new NotificationSnapshot(
                TimeState.None,
                CreateIntervalKey(TimeState.None, nextItem, now),
                $"下一节 · {nextSubjectName}",
                $"{FormatTime(nextItem.StartTime)} 开始",
                FormatTimeRange(nextItem),
                $">{nextSubjectName} -{GetDeltaString(now, nextItem.StartTime)}",
                totalSeconds,
                progressSeconds,
                progressPercent,
                remainingText);
        }

        return new NotificationSnapshot(
            TimeState.None,
            $"none:{now:yyyyMMdd}",
            "当前无课程",
            "ClassIsland 正在后台运行",
            string.Empty,
            "");
    }

    private string CreateNextClassText(bool includeStartTime)
    {
        var nextItem = LessonsService!.NextClassTimeLayoutItem;
        if (ReferenceEquals(nextItem, TimeLayoutItem.Empty) ||
            nextItem.EndTime <= nextItem.StartTime)
        {
            return string.Empty;
        }

        var name = GetSubjectName(LessonsService.NextClassSubject, string.Empty);
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        return includeStartTime
            ? $"下一节：{name} {FormatTime(nextItem.StartTime)}"
            : $"下一节：{name}";
    }

    public override void OnDestroy()
    {
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

        base.OnDestroy();
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
        var builder = new NotificationCompat.Builder(this, NotificationChannelId);
        builder.SetSmallIcon(ResourceConstant.Drawable.ic_logo_monochrome);
        builder.SetContentTitle(snapshot.Title);
        builder.SetContentText(snapshot.Content);
        builder.SetOngoing(true);
        builder.SetOnlyAlertOnce(true);
        builder.SetSilent(true);
        builder.SetPriority(NotificationCompat.PriorityHigh);
        builder.SetCategory(NotificationCompat.CategoryProgress);
        builder.SetColorized(false);
        builder.SetContentIntent(CreateContentIntent());
        builder.SetDeleteIntent(CreateDeleteIntent(snapshot.IntervalKey));

        if (!string.IsNullOrWhiteSpace(snapshot.SubText))
        {
            builder.SetSubText(snapshot.SubText);
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Baklava && snapshot.HasProgress)
        {
            ConfigureLiveUpdate(builder, snapshot);
        }
        else if (Build.VERSION.SdkInt < BuildVersionCodes.Baklava)
        {
            ConfigureCustomNotification(builder, snapshot);
        }

        return builder.Build()!;
    }

    private void ConfigureLiveUpdate(
        NotificationCompat.Builder builder,
        NotificationSnapshot snapshot)
    {
        var style = new NotificationCompat.ProgressStyle();
        style.SetProgressSegments(
        [
            new NotificationCompat.ProgressStyle.Segment(snapshot.ProgressMax)
        ]);
        style.SetProgress(snapshot.Progress);

        builder.SetStyle(style);
        builder.SetRequestPromotedOngoing(true);
        builder.SetWhen(
            Java.Lang.JavaSystem.CurrentTimeMillis() +
            snapshot.RemainingSeconds * 1000L);
        builder.SetShortCriticalText(snapshot.ShortTitle);
        builder.SetShowWhen(true);
        builder.SetUsesChronometer(true);
        builder.SetChronometerCountDown(true);
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
            snapshot.Title);
        compactViews.SetTextViewText(
            ResourceConstant.Id.notification_content,
            snapshot.Content);
        expandedViews.SetTextViewText(
            ResourceConstant.Id.notification_title,
            snapshot.Title);
        expandedViews.SetTextViewText(
            ResourceConstant.Id.notification_content,
            snapshot.Content);

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
            compactViews.SetViewVisibility(
                ResourceConstant.Id.notification_content,
                ViewStates.Gone);
            compactViews.SetViewVisibility(
                ResourceConstant.Id.notification_remaining,
                ViewStates.Visible);
            compactViews.SetTextViewText(
                ResourceConstant.Id.notification_remaining,
                $"剩余 {snapshot.RemainingText}");
            compactViews.SetProgressBar(
                ResourceConstant.Id.notification_progress,
                snapshot.ProgressMax,
                snapshot.Progress,
                false);

            expandedViews.SetTextViewText(
                ResourceConstant.Id.notification_time_range,
                snapshot.SubText);
            expandedViews.SetTextViewText(
                ResourceConstant.Id.notification_progress_percent,
                $"{snapshot.ProgressPercent}%");
            expandedViews.SetTextViewText(
                ResourceConstant.Id.notification_remaining,
                $"剩余 {snapshot.RemainingText}");
            expandedViews.SetProgressBar(
                ResourceConstant.Id.notification_progress,
                snapshot.ProgressMax,
                snapshot.Progress,
                false);

            builder.SetProgress(
                snapshot.ProgressMax,
                snapshot.Progress,
                false);
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

    private static string GetSubjectName(
        Subject? subject,
        string fallback)
    {
        if (subject == null ||
            ReferenceEquals(subject, Subject.Fallback) ||
            string.IsNullOrWhiteSpace(subject.Name) ||
            subject.Name == Subject.Fallback.Name)
        {
            return fallback;
        }

        return subject.Name.Trim();
    }

    private static string FormatTimeRange(TimeLayoutItem item) =>
        $"{FormatTime(item.StartTime)}–{FormatTime(item.EndTime)}";

    private static string FormatTime(TimeSpan time) =>
        time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static string FormatDuration(int seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private sealed record NotificationSnapshot(
        TimeState State,
        string IntervalKey,
        string Title,
        string Content,
        string SubText,
        string ShortTitle,
        int ProgressMax = 0,
        int Progress = 0,
        int ProgressPercent = 0,
        string RemainingText = "")
    {
        public bool HasProgress => ProgressMax > 0;

        public int RemainingSeconds => HasProgress
            ? Math.Max(0, ProgressMax - Progress)
            : 0;

        public static NotificationSnapshot Loading { get; } = new(
            TimeState.None,
            "loading",
            "ClassIsland",
            "正在加载课程状态…",
            string.Empty,
            "...");
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
