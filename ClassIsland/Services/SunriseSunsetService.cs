using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction;

namespace ClassIsland.Services;

/// <summary>
/// 提供统一的日出日落查询，并在影响时间表的数据或时钟发生变化时通知调用方重新调度。
/// </summary>
public sealed class SunriseSunsetService
{
    private readonly SettingsService _settingsService;
    private readonly IExactTimeService _exactTimeService;
    private readonly WeatherService _weatherService;
    private readonly Stopwatch _systemClockStopwatch = Stopwatch.StartNew();
    private Settings _settings;
    private DateTimeOffset _systemClockBaseline = DateTimeOffset.Now;

    private DispatcherTimer SystemClockMonitorTimer { get; } = new()
    {
        Interval = TimeSpan.FromMinutes(1)
    };

    public event EventHandler? ScheduleChanged;

    public bool HasFreshForecast => _weatherService.IsWeatherRefreshed;

    public SunriseSunsetService(
        SettingsService settingsService,
        IExactTimeService exactTimeService,
        WeatherService weatherService)
    {
        _settingsService = settingsService;
        _exactTimeService = exactTimeService;
        _weatherService = weatherService;
        _settings = settingsService.Settings;

        _weatherService.WeatherRefreshed += OnScheduleSourceChanged;
        _exactTimeService.PropertyChanged += OnExactTimeServicePropertyChanged;
        _settingsService.PropertyChanged += OnSettingsServicePropertyChanged;
        _settings.PropertyChanged += OnSettingsPropertyChanged;
        PlatformServices.SystemEventsService.TimeChanged += OnSystemTimeChanged;
        AppBase.Current.AppStopping += OnAppStopping;

        SystemClockMonitorTimer.Tick += OnSystemClockMonitorTick;
        SystemClockMonitorTimer.Start();
    }

    public DateTimeOffset GetCurrentTime()
    {
        return new DateTimeOffset(_exactTimeService.GetCurrentLocalDateTime());
    }

    public bool IsDaylight()
    {
        return SunriseSunsetSchedule.TryGetDaylightStatus(
                   _settings.LastWeatherInfo.ForecastDaily.SunRiseSet.Value,
                   GetCurrentTime(),
                   out var isDaylight)
               && isDaylight;
    }

    public bool TryGetNextTransition(SunTransition transition, out DateTimeOffset nextTransition)
    {
        var now = GetCurrentTime();
        return SunriseSunsetSchedule.TryGetNextTransition(
            _settings.LastWeatherInfo.ForecastDaily.SunRiseSet.Value,
            now,
            transition,
            null,
            out nextTransition);
    }

    internal bool TryGetNextTransition(
        SunTransition transition,
        DateTimeOffset now,
        IReadOnlySet<DateOnly> excludedForecastDates,
        out DateTimeOffset nextTransition)
    {
        return SunriseSunsetSchedule.TryGetNextTransition(
            _settings.LastWeatherInfo.ForecastDaily.SunRiseSet.Value,
            now,
            transition,
            excludedForecastDates,
            out nextTransition);
    }

    internal bool TryGetLatestTransitionAtOrBefore(
        SunTransition transition,
        DateOnly earliestForecastDate,
        DateTimeOffset atOrBefore,
        IReadOnlySet<DateOnly> excludedForecastDates,
        out DateTimeOffset latestTransition)
    {
        return SunriseSunsetSchedule.TryGetLatestTransitionAtOrBefore(
            _settings.LastWeatherInfo.ForecastDaily.SunRiseSet.Value,
            transition,
            earliestForecastDate,
            atOrBefore,
            excludedForecastDates,
            out latestTransition);
    }

    internal bool TryGetLatestTransitionBetween(
        SunTransition transition,
        DateTimeOffset afterExclusive,
        DateTimeOffset atOrBefore,
        IReadOnlySet<DateOnly> excludedForecastDates,
        out DateTimeOffset latestTransition)
    {
        return SunriseSunsetSchedule.TryGetLatestTransitionBetween(
            _settings.LastWeatherInfo.ForecastDaily.SunRiseSet.Value,
            transition,
            afterExclusive,
            atOrBefore,
            excludedForecastDates,
            out latestTransition);
    }

    private void OnScheduleSourceChanged(object? sender, EventArgs e)
    {
        RaiseScheduleChanged();
    }

    private void OnExactTimeServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaiseScheduleChanged();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Settings.IsExactTimeEnabled)
            or nameof(Settings.TimeOffsetSeconds)
            or nameof(Settings.DebugTimeOffsetSeconds))
        {
            RaiseScheduleChanged();
        }
    }

    private void OnSettingsServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SettingsService.Settings))
        {
            return;
        }

        _settings.PropertyChanged -= OnSettingsPropertyChanged;
        _settings = _settingsService.Settings;
        _settings.PropertyChanged += OnSettingsPropertyChanged;
        RaiseScheduleChanged();
    }

    private void OnSystemTimeChanged(object? sender, EventArgs e)
    {
        ResetSystemClockBaseline();
        RaiseScheduleChanged();
    }

    private void OnSystemClockMonitorTick(object? sender, EventArgs e)
    {
        var currentSystemTime = DateTimeOffset.Now;
        var expectedSystemTime = _systemClockBaseline + _systemClockStopwatch.Elapsed;
        var clockChanged = Math.Abs((currentSystemTime.UtcDateTime - expectedSystemTime.UtcDateTime).TotalSeconds) > 5;
        var timeZoneChanged = currentSystemTime.Offset != _systemClockBaseline.Offset;

        ResetSystemClockBaseline(currentSystemTime);
        if (clockChanged || timeZoneChanged)
        {
            RaiseScheduleChanged();
        }
    }

    private void ResetSystemClockBaseline(DateTimeOffset? currentSystemTime = null)
    {
        _systemClockBaseline = currentSystemTime ?? DateTimeOffset.Now;
        _systemClockStopwatch.Restart();
    }

    private void RaiseScheduleChanged()
    {
        ScheduleChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnAppStopping(object? sender, EventArgs e)
    {
        SystemClockMonitorTimer.Stop();
        SystemClockMonitorTimer.Tick -= OnSystemClockMonitorTick;
        _weatherService.WeatherRefreshed -= OnScheduleSourceChanged;
        _exactTimeService.PropertyChanged -= OnExactTimeServicePropertyChanged;
        _settingsService.PropertyChanged -= OnSettingsServicePropertyChanged;
        _settings.PropertyChanged -= OnSettingsPropertyChanged;
        PlatformServices.SystemEventsService.TimeChanged -= OnSystemTimeChanged;
        AppBase.Current.AppStopping -= OnAppStopping;
    }
}
