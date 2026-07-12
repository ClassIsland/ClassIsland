using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services.LiveActivities;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using Foundation;
using UIKit;

namespace ClassIsland.iOS.Services.LiveActivities;

/// <summary>
/// 将共享课程状态发布到 ActivityKit；倒计时由系统按绝对起止时间持续渲染。
/// </summary>
internal sealed class LessonsLiveActivityCoordinator(
    ILiveActivityService liveActivityService) : IDisposable
{
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _cancellation = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private NSObject? _foregroundObserver;
    private ILessonsService? _lessonsService;
    private LessonsLiveActivitySnapshotFactory? _snapshotFactory;
    private LessonLiveActivityContent? _lastRequestedContent;
    private LessonLiveActivityContent? _lastFailedContent;
    private DateTimeOffset _retryFailedContentAfter;
    private bool _isStarted;
    private bool _isWorkStarted;
    private int _isStopping;

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;
        AppBase.Current.AppStarted += OnAppStarted;
        AppBase.Current.AppStopping += OnAppStopping;
        _foregroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillEnterForegroundNotification,
            _ =>
            {
                _lastRequestedContent = null;
                _lastFailedContent = null;
                _retryFailedContentAfter = default;
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
        var exactTimeService = IAppHost.GetService<IExactTimeService>();
        _snapshotFactory = new LessonsLiveActivitySnapshotFactory(
            _lessonsService,
            exactTimeService);
        _lessonsService.PostMainTimerTicked += OnPostMainTimerTicked;
        _isWorkStarted = true;
        QueueRefresh();
    }

    private void OnPostMainTimerTicked(object? sender, EventArgs e) => QueueRefresh();

    private async void QueueRefresh()
    {
        if (!_isWorkStarted ||
            _snapshotFactory == null ||
            _cancellation.IsCancellationRequested ||
            Volatile.Read(ref _isStopping) != 0)
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
            if (content == _lastRequestedContent)
            {
                return;
            }

            if (content == _lastFailedContent &&
                DateTimeOffset.UtcNow < _retryFailedContentAfter)
            {
                return;
            }

            var result = await liveActivityService.PublishAsync(
                content,
                CancellationToken.None);
            if (result.IsSuccess || result.Code is
                    LiveActivityResultCode.Disabled or LiveActivityResultCode.Unsupported)
            {
                // 系统禁用或不支持时也避免被 50 ms 主计时器反复调用；
                // 回到前台会清除此值并重新检查系统授权。
                _lastRequestedContent = content;
                _lastFailedContent = null;
                _retryFailedContentAfter = default;
            }
            else
            {
                RememberFailure(content);
                Console.Error.WriteLine(
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
                RememberFailure(content);
            }

            Console.Error.WriteLine($"更新课程实时活动时发生异常：{exception.Message}");
        }
        finally
        {
            if (gateEntered)
            {
                _refreshGate.Release();
            }
        }
    }

    private async void OnAppStopping(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _isStopping, 1) != 0)
        {
            return;
        }

        var gateEntered = false;
        try
        {
            // 等待在途 Publish 的原生 callback，再发出 End，保证 ActivityKit 操作顺序。
            await _refreshGate.WaitAsync(CancellationToken.None);
            gateEntered = true;
            await liveActivityService.EndAsync(
                LiveActivityDismissalPolicy.Immediate,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"结束课程实时活动时发生异常：{exception.Message}");
        }
        finally
        {
            if (gateEntered)
            {
                _refreshGate.Release();
            }

            _cancellation.Cancel();
        }
    }

    private void RememberFailure(LessonLiveActivityContent content)
    {
        _lastFailedContent = content;
        _retryFailedContentAfter = DateTimeOffset.UtcNow + FailureRetryDelay;
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
            snapshot.EndTime);
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

    public void Dispose()
    {
        if (!_isStarted)
        {
            return;
        }

        AppBase.Current.AppStarted -= OnAppStarted;
        AppBase.Current.AppStopping -= OnAppStopping;
        if (_lessonsService != null)
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
        // QueueRefresh 是 async void，此时可能仍在 finally 中释放信号量。
        // 同步对象不持有外部资源，交由 GC 回收可避免停止阶段的 Dispose/Release 竞态。
        _isStarted = false;
        _isWorkStarted = false;
    }
}
