using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ClassIsland.Services.Automation.Triggers;

/// <summary>
/// 管理单个日出或日落触发器的调度生命周期。
/// </summary>
internal sealed class SunriseSunsetTriggerScheduler
{
    private readonly SunriseSunsetService _sunriseSunsetService;
    private readonly SunTransition _transition;
    private readonly Action _trigger;
    private readonly HashSet<DateOnly> _triggeredForecastDates = [];
    private CancellationTokenSource? _delayCancellationTokenSource;
    private DateTimeOffset? _lastScheduleObservationTime;
    private DateTimeOffset? _scheduledTarget;
    private bool _isLoaded;

    public SunriseSunsetTriggerScheduler(
        SunriseSunsetService sunriseSunsetService,
        SunTransition transition,
        Action trigger)
    {
        _sunriseSunsetService = sunriseSunsetService;
        _transition = transition;
        _trigger = trigger;
    }

    public void Load()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        _sunriseSunsetService.ScheduleChanged += OnScheduleChanged;
        RescheduleOnUiThread();
    }

    public void Unload()
    {
        if (!_isLoaded)
        {
            return;
        }

        _isLoaded = false;
        _sunriseSunsetService.ScheduleChanged -= OnScheduleChanged;
        _delayCancellationTokenSource?.Cancel();
        _delayCancellationTokenSource?.Dispose();
        _delayCancellationTokenSource = null;
        _lastScheduleObservationTime = null;
        _scheduledTarget = null;
    }

    private void OnScheduleChanged(object? sender, EventArgs e)
    {
        RescheduleOnUiThread();
    }

    private void RescheduleOnUiThread()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ScheduleNext();
            return;
        }

        _ = Dispatcher.UIThread.InvokeAsync(ScheduleNext);
    }

    private void ScheduleNext()
    {
        var now = _sunriseSunsetService.GetCurrentTime();
        var previousScheduledTarget = _scheduledTarget;

        _delayCancellationTokenSource?.Cancel();
        _delayCancellationTokenSource?.Dispose();
        _delayCancellationTokenSource = null;
        _scheduledTarget = null;

        if (TryGetCrossedTarget(now, previousScheduledTarget, out var crossedTarget))
        {
            TriggerTarget(crossedTarget);
        }

        if (!_isLoaded)
        {
            return;
        }

        _lastScheduleObservationTime = now;
        if (!_sunriseSunsetService.HasFreshForecast ||
            !_sunriseSunsetService.TryGetNextTransition(
                _transition,
                now,
                _triggeredForecastDates,
                out var target))
        {
            return;
        }

        var delay = target - _sunriseSunsetService.GetCurrentTime();
        if (delay <= TimeSpan.Zero)
        {
            TriggerTarget(target);
            RescheduleOnUiThread();
            return;
        }

        _delayCancellationTokenSource = new CancellationTokenSource();
        _scheduledTarget = target;
        _ = WaitAndTriggerAsync(delay, _delayCancellationTokenSource.Token);
    }

    private bool TryGetCrossedTarget(
        DateTimeOffset now,
        DateTimeOffset? previousScheduledTarget,
        out DateTimeOffset crossedTarget)
    {
        crossedTarget = default;
        if (!_isLoaded)
        {
            return false;
        }

        DateTimeOffset? candidate = null;
        if (_sunriseSunsetService.HasFreshForecast && _lastScheduleObservationTime != null &&
            _sunriseSunsetService.TryGetLatestTransitionBetween(
                _transition,
                _lastScheduleObservationTime.Value,
                now,
                _triggeredForecastDates,
                out var observedCrossedTarget))
        {
            // 观察窗口允许此前缺失或损坏的较早预报日期在恢复后补触发。
            candidate = observedCrossedTarget;
        }

        if (_sunriseSunsetService.HasFreshForecast && previousScheduledTarget != null &&
            _sunriseSunsetService.TryGetLatestTransitionAtOrBefore(
                _transition,
                DateOnly.FromDateTime(previousScheduledTarget.Value.Date),
                now,
                _triggeredForecastDates,
                out var revisedCrossedTarget) &&
            (candidate == null || revisedCrossedTarget > candidate.Value))
        {
            // 旧目标日期补充覆盖边界被修订到观察窗口之前的情况。
            candidate = revisedCrossedTarget;
        }

        if (candidate != null)
        {
            crossedTarget = candidate.Value;
            return true;
        }

        if (previousScheduledTarget == null || previousScheduledTarget > now)
        {
            return false;
        }

        // 刷新或校时可能恰好抢在到期回调前执行，此时补上已跨过但已从预报中移除的旧边界。
        crossedTarget = previousScheduledTarget.Value;
        return true;
    }

    private async Task WaitAndTriggerAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_isLoaded || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var triggeredTarget = _scheduledTarget;
                _scheduledTarget = null;
                if (triggeredTarget != null)
                {
                    TriggerTarget(triggeredTarget.Value);
                }
                ScheduleNext();
            });
        }
        catch (OperationCanceledException)
        {
            // 影响日出日落时间的数据已变化，旧调度被新调度替代。
        }
    }

    private void TriggerTarget(DateTimeOffset target)
    {
        if (!_triggeredForecastDates.Add(DateOnly.FromDateTime(target.Date)))
        {
            return;
        }

        _trigger();
    }
}
