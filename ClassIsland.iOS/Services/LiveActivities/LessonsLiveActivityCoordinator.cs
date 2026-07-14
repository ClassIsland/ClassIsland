using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.iOS.Services.Notifications;
using ClassIsland.Services;
using ClassIsland.Services.LiveActivities;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace ClassIsland.iOS.Services.LiveActivities;

/// <summary>
/// 将共享课程状态发布到 ActivityKit；倒计时由系统按绝对起止时间持续渲染。
/// </summary>
internal sealed class LessonsLiveActivityCoordinator(
    ILiveActivityService liveActivityService) : IDisposable
{
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ManualTerminationTimeout = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _cancellation = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private NSObject? _foregroundObserver;
    private ILessonsService? _lessonsService;
    private LessonsLiveActivitySnapshotFactory? _snapshotFactory;
    private IosLessonNotificationScheduleFactory? _notificationScheduleFactory;
    private ILogger<LessonsLiveActivityCoordinator>? _logger;
    private LessonLiveActivityContent? _lastRequestedContent;
    private DateTimeOffset _retryAfter;
    private bool _activityStateKnown;
    private bool _hasVisibleActivity;
    private bool _isAvailabilityUnavailable;
    private bool _isStarted;
    private bool _isWorkStarted;
    private int _isStopping;
    private long _lastTimerRefreshSecond = long.MinValue;

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;
        AppBase.Current.AppStarted += OnAppStarted;
        _foregroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillEnterForegroundNotification,
            _ =>
            {
                _lastRequestedContent = null;
                _retryAfter = default;
                _activityStateKnown = false;
                _isAvailabilityUnavailable = false;
                QueueRefresh();
            });

        if (AppBase.CurrentLifetime == ApplicationLifetime.Running)
        {
            StartWork();
        }
    }

    private void OnAppStarted(object? sender, EventArgs e) => StartWork();

    private void StartWork()
    {
        if (_isWorkStarted || Volatile.Read(ref _isStopping) != 0)
        {
            return;
        }

        _lessonsService = IAppHost.GetService<ILessonsService>();
        _logger = IAppHost.TryGetService<ILogger<LessonsLiveActivityCoordinator>>();
        var exactTimeService = IAppHost.GetService<IExactTimeService>();
        _snapshotFactory = new LessonsLiveActivitySnapshotFactory(
            _lessonsService,
            exactTimeService);
        _notificationScheduleFactory = new IosLessonNotificationScheduleFactory(
            _lessonsService,
            IAppHost.GetService<IProfileService>(),
            IAppHost.GetService<INotificationHostService>(),
            IAppHost.GetService<SettingsService>(),
            exactTimeService);
        _lessonsService.PostMainTimerTicked += OnPostMainTimerTicked;
        _isWorkStarted = true;
        LogInformation($"课程实时活动协调器已启动：Availability={liveActivityService.Availability}。");
        QueueRefresh();
    }

    private void OnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var currentSecond = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Interlocked.Exchange(ref _lastTimerRefreshSecond, currentSecond) ==
            currentSecond)
        {
            return;
        }

        QueueRefresh();
    }

    private void QueueRefresh() => _ = QueueRefreshAsync();

    private async Task QueueRefreshAsync()
    {
        if (!_isWorkStarted ||
            _snapshotFactory == null ||
            _cancellation.IsCancellationRequested ||
            Volatile.Read(ref _isStopping) != 0 ||
            _isAvailabilityUnavailable)
        {
            return;
        }

        var gateEntered = false;
        LessonLiveActivityContent? content = null;
        try
        {
            if (!await _refreshGate.WaitAsync(0, _cancellation.Token))
            {
                return;
            }

            gateEntered = true;
            if (Volatile.Read(ref _isStopping) != 0)
            {
                return;
            }

            content = CreateContent(_snapshotFactory.Create());
            if (DateTimeOffset.UtcNow < _retryAfter)
            {
                return;
            }

            var preparationNotificationTime = content.IsUpcomingLesson
                ? _notificationScheduleFactory?.GetUpcomingClassPreparationTime()
                : null;
            content = LessonLiveActivityPublicationPolicy.AlignUpcomingProgressStart(
                content,
                preparationNotificationTime);
            if (!LessonLiveActivityPublicationPolicy.ShouldPublish(
                    content,
                    DateTimeOffset.Now,
                    preparationNotificationTime))
            {
                if (_activityStateKnown && !_hasVisibleActivity)
                {
                    return;
                }

                var endResult = await liveActivityService.EndAsync(
                    LiveActivityDismissalPolicy.Immediate,
                    _cancellation.Token);
                if (endResult.IsSuccess)
                {
                    _lastRequestedContent = null;
                    _activityStateKnown = true;
                    _hasVisibleActivity = false;
                    _retryAfter = default;
                    LogInformation(
                        $"当前没有有效课程区间，已结束课程实时活动：" +
                        $"ActivityId={endResult.ActivityId ?? "<null>"}。");
                }
                else if (RememberUnavailable(endResult))
                {
                    LogWarning(
                        $"课程实时活动不可用：{endResult.Code} {endResult.ErrorMessage}");
                }
                else
                {
                    RememberFailure();
                    LogError(
                        $"无法结束课程实时活动：{endResult.Code} {endResult.ErrorMessage}");
                }
                return;
            }

            if (_activityStateKnown &&
                _hasVisibleActivity &&
                content == _lastRequestedContent)
            {
                return;
            }

            var result = await liveActivityService.PublishAsync(
                content,
                _cancellation.Token);
            if (result.IsSuccess)
            {
                _lastRequestedContent = content;
                _activityStateKnown = true;
                _hasVisibleActivity = true;
                _retryAfter = default;
                LogInformation(
                    $"课程实时活动已发布：ActivityId={result.ActivityId ?? "<null>"}，" +
                    $"IntervalId={content.IntervalId}，Phase={content.Phase}。");
            }
            else if (RememberUnavailable(result))
            {
                LogWarning(
                    $"课程实时活动不可用：{result.Code} {result.ErrorMessage}");
            }
            else
            {
                RememberFailure();
                LogError(
                    $"无法更新课程实时活动：{result.Code} {result.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            // 应用正在停止。
        }
        catch (Exception exception)
        {
            if (content != null)
            {
                RememberFailure();
            }

            LogError("更新课程实时活动时发生异常。", exception);
        }
        finally
        {
            if (gateEntered)
            {
                _refreshGate.Release();
            }
        }
    }

    private bool RememberUnavailable(LiveActivityResult result)
    {
        if (result.Code is not (LiveActivityResultCode.Disabled or
            LiveActivityResultCode.Unsupported))
        {
            return false;
        }

        _activityStateKnown = true;
        _hasVisibleActivity = false;
        _isAvailabilityUnavailable = true;
        _retryAfter = default;
        return true;
    }

    private void RememberFailure()
    {
        _retryAfter = DateTimeOffset.UtcNow + FailureRetryDelay;
    }

    private void LogInformation(string message)
    {
        if (_logger != null)
        {
            _logger.LogInformation("{Message}", message);
            return;
        }

        Console.WriteLine(message);
    }

    private void LogWarning(string message)
    {
        if (_logger != null)
        {
            _logger.LogWarning("{Message}", message);
            return;
        }

        Console.Error.WriteLine(message);
    }

    private void LogError(string message, Exception? exception = null)
    {
        if (_logger != null)
        {
            _logger.LogError(exception, "{Message}", message);
            return;
        }

        Console.Error.WriteLine(exception == null ? message : $"{message}\n{exception}");
    }

    private static LessonLiveActivityContent CreateContent(
        LessonsLiveActivitySnapshot snapshot)
    {
        var subtitle = RemoveRenderedCountdown(snapshot.Content, snapshot.RemainingText);
        var compactText = snapshot.Title.Contains('·')
            ? snapshot.Title[(snapshot.Title.IndexOf('·') + 1)..].Trim()
            : snapshot.Title;

        return new LessonLiveActivityContent(
            snapshot.IntervalKey,
            snapshot.State switch
            {
                TimeState.OnClass => LessonLiveActivityPhase.OnClass,
                TimeState.Breaking => LessonLiveActivityPhase.Breaking,
                TimeState.AfterSchool => LessonLiveActivityPhase.AfterSchool,
                _ => LessonLiveActivityPhase.None
            },
            snapshot.Title,
            subtitle,
            snapshot.SubText,
            compactText,
            snapshot.StartTime,
            snapshot.EndTime,
            IsUpcomingLesson: snapshot.IsUpcomingLesson);
    }

    private static string RemoveRenderedCountdown(string content, string remainingText)
    {
        if (string.IsNullOrEmpty(remainingText))
        {
            return content;
        }

        var standalone = $"剩余 {remainingText}";
        if (content == standalone)
        {
            return string.Empty;
        }

        var suffix = $" · {standalone}";
        return content.EndsWith(suffix, StringComparison.Ordinal)
            ? content[..^suffix.Length]
            : content;
    }

    public async Task StopAndEndAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _isStopping, 1, 0) != 0)
        {
            return;
        }

        if (_lessonsService != null)
        {
            _lessonsService.PostMainTimerTicked -= OnPostMainTimerTicked;
        }
        _isWorkStarted = false;
        _cancellation.Cancel();

        var gateEntered = false;
        using var timeoutCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(ManualTerminationTimeout);
        try
        {
            await _refreshGate.WaitAsync(timeoutCancellation.Token);
            gateEntered = true;
            var result = await liveActivityService.EndAsync(
                LiveActivityDismissalPolicy.Immediate,
                timeoutCancellation.Token);
            if (result.IsSuccess)
            {
                _lastRequestedContent = null;
                _activityStateKnown = true;
                _hasVisibleActivity = false;
                LogInformation(
                    $"用户准备手动结束应用，已关闭课程实时活动：" +
                    $"ActivityId={result.ActivityId ?? "<null>"}。");
            }
            else if (result.Code is not (LiveActivityResultCode.Unsupported or
                         LiveActivityResultCode.Disabled))
            {
                LogError(
                    $"用户准备手动结束应用，但实时活动关闭失败：" +
                    $"{result.Code} {result.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            LogWarning(
                $"在 {ManualTerminationTimeout.TotalSeconds:0} 秒内未能结束实时活动，" +
                "将继续显示手动结束应用提示。");
        }
        finally
        {
            if (gateEntered)
            {
                _refreshGate.Release();
            }
        }
    }

    public void Dispose()
    {
        if (!_isStarted)
        {
            return;
        }

        AppBase.Current.AppStarted -= OnAppStarted;
        if (_lessonsService != null && Volatile.Read(ref _isStopping) == 0)
        {
            _lessonsService.PostMainTimerTicked -= OnPostMainTimerTicked;
        }

        if (_foregroundObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_foregroundObserver);
            _foregroundObserver.Dispose();
            _foregroundObserver = null;
        }

        Interlocked.Exchange(ref _isStopping, 1);
        _cancellation.Cancel();
        // 后台刷新此时可能仍在 finally 中释放信号量。同步对象不持有外部资源，
        // 交由 GC 回收可避免停止阶段的 Dispose/Release 竞态。
        _isStarted = false;
        _isWorkStarted = false;
    }
}
