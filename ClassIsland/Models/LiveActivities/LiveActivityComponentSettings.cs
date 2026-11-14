using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.LiveActivities;

public enum LiveActivityComponentKind
{
    Schedule,
    Date,
    Clock,
    Weather,
    Countdown,
    Text,
    Separator
}

public sealed record LiveActivityComponentOption(
    LiveActivityComponentKind Kind,
    string DisplayName);

public sealed partial class LiveActivityComponentSettings : ObservableObject
{
    public const int MaximumComponents = 6;

    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private bool _showNextLesson = true;
    [ObservableProperty] private bool _showProgress = true;
    [ObservableProperty] private bool _useCustomCountdown;
    [ObservableProperty] private string _countdownName = "倒计时";
    [ObservableProperty] private DateTime? _targetDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName), nameof(Description), nameof(IsTextComponent),
        nameof(IsScheduleComponent), nameof(IsCountdownComponent))]
    private LiveActivityComponentKind _kind;

    [JsonIgnore]
    public string DisplayName => Kind switch
    {
        LiveActivityComponentKind.Schedule => "课程表",
        LiveActivityComponentKind.Date => "日期",
        LiveActivityComponentKind.Clock => "时钟",
        LiveActivityComponentKind.Weather => "天气简报",
        LiveActivityComponentKind.Countdown => "倒计时",
        LiveActivityComponentKind.Text => "文本",
        LiveActivityComponentKind.Separator => "分割线",
        _ => "未知组件"
    };

    [JsonIgnore] public bool IsTextComponent => Kind == LiveActivityComponentKind.Text;
    [JsonIgnore] public bool IsScheduleComponent => Kind == LiveActivityComponentKind.Schedule;
    [JsonIgnore] public bool IsCountdownComponent => Kind == LiveActivityComponentKind.Countdown;

    [JsonIgnore]
    public string Description => Kind switch
    {
        LiveActivityComponentKind.Schedule => "当前课程、下一节课程和进度。",
        LiveActivityComponentKind.Date => "显示当前日期和星期。",
        LiveActivityComponentKind.Clock => "显示当前时间。",
        LiveActivityComponentKind.Weather => "显示当前温度和天气状态。",
        LiveActivityComponentKind.Countdown => "显示课程剩余时间，或倒计时到自定义日期。",
        LiveActivityComponentKind.Text => "显示一段短文本。",
        LiveActivityComponentKind.Separator => "在组件之间显示分隔线。",
        _ => string.Empty
    };

    public static ObservableCollection<LiveActivityComponentSettings> CreateDefault() =>
    [
        new() { Kind = LiveActivityComponentKind.Schedule },
        new() { Kind = LiveActivityComponentKind.Date },
        new() { Kind = LiveActivityComponentKind.Weather },
        new() { Kind = LiveActivityComponentKind.Countdown }
    ];

    public static IReadOnlyList<LiveActivityComponentOption> AvailableKinds { get; } =
    [
        new(LiveActivityComponentKind.Schedule, "课程表"),
        new(LiveActivityComponentKind.Date, "日期"),
        new(LiveActivityComponentKind.Clock, "时钟"),
        new(LiveActivityComponentKind.Weather, "天气简报"),
        new(LiveActivityComponentKind.Countdown, "倒计时"),
        new(LiveActivityComponentKind.Text, "文本"),
        new(LiveActivityComponentKind.Separator, "分割线")
    ];

    public static ObservableCollection<LiveActivityComponentSettings> Normalize(
        IEnumerable<LiveActivityComponentSettings>? components)
    {
        var normalized = components?
            .Where(x => x != null && Enum.IsDefined(x.Kind))
            .Take(MaximumComponents)
            .ToList();
        return normalized == null
            ? CreateDefault()
            : new ObservableCollection<LiveActivityComponentSettings>(normalized);
    }
}
