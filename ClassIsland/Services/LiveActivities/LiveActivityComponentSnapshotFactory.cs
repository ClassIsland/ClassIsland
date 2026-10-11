using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassIsland.Core.Models.Weather;
using ClassIsland.Models;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Platforms.Abstraction.Models.LiveActivities;
using ClassIsland.Shared.Enums;

namespace ClassIsland.Services.LiveActivities;

internal static class LiveActivityComponentSnapshotFactory
{
    private static readonly CultureInfo ChineseCulture = CultureInfo.GetCultureInfo("zh-CN");

    public static LessonLiveActivityComponent[]? Create(
        Settings settings,
        LessonsLiveActivitySnapshot snapshot,
        DateTime now,
        string weatherText,
        LessonLiveActivityDetails? nextLesson)
    {
        if (!settings.IsLiveActivityLayoutCustomized)
        {
            return null;
        }

        var components = new List<LessonLiveActivityComponent>();
        foreach (var component in settings.LiveActivityComponents
                     .Where(x => x != null && x.IsEnabled && Enum.IsDefined(x.Kind))
                     .Take(LiveActivityComponentSettings.MaximumComponents))
        {
            var display = component.Kind switch
            {
                LiveActivityComponentKind.Schedule => CreateSchedule(component, snapshot, nextLesson),
                LiveActivityComponentKind.Date => new LessonLiveActivityComponent(
                    "date", now.ToString("M月d日 ddd", ChineseCulture), SymbolName: "calendar"),
                LiveActivityComponentKind.Clock => new LessonLiveActivityComponent(
                    "clock", SymbolName: "clock",
                    TargetTime: new DateTimeOffset(now.Date).ToUnixTimeSeconds()),
                LiveActivityComponentKind.Weather => CreateWeather(settings.LastWeatherInfo, weatherText),
                LiveActivityComponentKind.Countdown => CreateCountdown(component),
                LiveActivityComponentKind.Text => new LessonLiveActivityComponent(
                    "text", TrimText(component.Text), SymbolName: "text.alignleft"),
                LiveActivityComponentKind.Separator => new LessonLiveActivityComponent("separator"),
                _ => null
            };
            if (display != null && (display.Kind == "separator" ||
                                    !string.IsNullOrEmpty(display.Text) || display.TargetTime != null))
            {
                components.Add(display);
            }
        }

        return components.ToArray();
    }

    private static LessonLiveActivityComponent CreateSchedule(
        LiveActivityComponentSettings component,
        LessonsLiveActivitySnapshot snapshot,
        LessonLiveActivityDetails? nextLesson)
    {
        var details = snapshot.ActivityDetails;
        var nextText = component.ShowNextLesson && snapshot.State == TimeState.OnClass && nextLesson != null
            ? $"下一节：{nextLesson.LessonName} {nextLesson.TimeText}"
            : string.Empty;
        return new LessonLiveActivityComponent(
            "schedule",
            TrimText(details?.LessonName ?? snapshot.Title),
            TrimText(string.Join(" · ", new[] { details?.Location, details?.TimeText }
                .Where(x => !string.IsNullOrWhiteSpace(x)))),
            "book.closed.fill",
            TrimText(nextText),
            ShowProgress: component.ShowProgress);
    }

    private static LessonLiveActivityComponent CreateWeather(WeatherInfo weather, string weatherText)
    {
        if (weather.UpdateTimeUnix <= 0 || string.IsNullOrWhiteSpace(weather.Current.Temperature.Value))
        {
            return new LessonLiveActivityComponent("weather", "天气暂不可用", SymbolName: "cloud");
        }

        return new LessonLiveActivityComponent(
            "weather",
            TrimText(weather.Current.Temperature.ToString()),
            TrimText(weatherText),
            WeatherSymbol(weatherText));
    }

    private static string WeatherSymbol(string weatherText) => weatherText switch
    {
        "晴" => "sun.max.fill",
        "多云" => "cloud.sun.fill",
        "阴" => "cloud.fill",
        _ when weatherText.Contains('雪') => "cloud.snow.fill",
        _ when weatherText.Contains('雷') => "cloud.bolt.rain.fill",
        _ when weatherText.Contains('雨') => "cloud.rain.fill",
        _ when weatherText.Contains('雾') || weatherText.Contains('霾') => "cloud.fog.fill",
        _ => "cloud.fill"
    };

    private static LessonLiveActivityComponent CreateCountdown(LiveActivityComponentSettings component)
    {
        if (!component.UseCustomCountdown)
        {
            // 实际剩余时长由系统根据课程区间渲染，不把每秒变化的字符串用于发布去重。
            return new LessonLiveActivityComponent("countdown", "剩余时间", SymbolName: "hourglass");
        }

        if (component.TargetDate is not { } target)
        {
            return new LessonLiveActivityComponent("text", "请设置倒计时日期", SymbolName: "hourglass");
        }

        var name = string.IsNullOrWhiteSpace(component.CountdownName) ? "倒计时" : component.CountdownName;
        if (target.Year is < 1900 or > 4000)
        {
            return new LessonLiveActivityComponent("text", "请选择有效的倒计时日期", SymbolName: "hourglass");
        }

        var targetTime = new DateTimeOffset(target.Date);
        return new LessonLiveActivityComponent(
            "countdown", TrimText(name), SymbolName: "hourglass",
            TargetTime: targetTime.ToUnixTimeSeconds());
    }

    internal static string TrimText(string? text)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        var elements = StringInfo.ParseCombiningCharacters(trimmed);
        return elements.Length <= 48 ? trimmed : trimmed[..elements[47]] + "…";
    }
}
