using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.sunset", "\u65e5\u843d\u65f6", "\uEC3F")]
public class SunsetTrigger : TriggerBase
{
    private CancellationTokenSource? _stopCancellationTokenSource;

    ILogger<SunsetTrigger> Logger { get; } = App.GetService<ILogger<SunsetTrigger>>();
    IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();
    IWeatherService WeatherService { get; } = App.GetService<IWeatherService>();
    SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    public override void Loaded()
    {
        WeatherService.PropertyChanged += WeatherServiceOnPropertyChanged;
        _stopCancellationTokenSource = new CancellationTokenSource();
        ScheduleNext();
    }

    private void WeatherServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WeatherService.IsWeatherRefreshed))
            ScheduleNext();
    }

    private void ScheduleNext()
    {
        if (_stopCancellationTokenSource?.IsCancellationRequested == false)
            _stopCancellationTokenSource.Cancel();

        if (!WeatherService.IsWeatherRefreshed)
            return;

        var now = ExactTimeService.GetCurrentLocalDateTime();
        if (!TryGetSunset(now, out var sunset))
            return;

        var target = sunset;
        if (now >= target)
            target = target.AddDays(1);

        var delay = target - now;

        _stopCancellationTokenSource = new CancellationTokenSource();
        Task.Delay(delay, _stopCancellationTokenSource.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            Dispatcher.UIThread.Invoke(() =>
            {
                Trigger();
                ScheduleNext();
            });
        });
    }

    private bool TryGetSunset(DateTimeOffset now, out DateTimeOffset sunset)
    {
        sunset = default;
        var list = SettingsService.Settings.LastWeatherInfo.ForecastDaily.SunRiseSet.Value;
        if (list == null || list.Count == 0) return false;

        foreach (var item in list)
        {
            if (!DateTimeOffset.TryParse(item.From, CultureInfo.InvariantCulture, DateTimeStyles.None, out var sr))
                continue;
            if (!DateTimeOffset.TryParse(item.To, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ss))
                continue;
            if (sr.Date == now.Date || ss.Date == now.Date)
            {
                sunset = ss;
                return true;
            }
        }
        return false;
    }

    public override void UnLoaded()
    {
        WeatherService.PropertyChanged -= WeatherServiceOnPropertyChanged;
        _stopCancellationTokenSource?.Cancel();
    }
}