using System;
using ClassIsland.Shared.Enums;

namespace ClassIsland.Models.LiveActivities;

internal sealed record LessonsLiveActivitySnapshot(
    TimeState State,
    string IntervalKey,
    string Title,
    string Content,
    string SubText,
    string ShortTitle,
    int ProgressMax = 0,
    int Progress = 0,
    int ProgressPercent = 0,
    string RemainingText = "",
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null,
    bool IsUpcomingLesson = false)
{
    public bool HasProgress => ProgressMax > 0;

    public int RemainingSeconds => HasProgress
        ? Math.Max(0, ProgressMax - Progress)
        : 0;

    public bool HasSameNotificationContent(
        LessonsLiveActivitySnapshot? other) =>
        other != null &&
        State == other.State &&
        IntervalKey == other.IntervalKey &&
        Title == other.Title &&
        Content == other.Content &&
        SubText == other.SubText &&
        ShortTitle == other.ShortTitle &&
        ProgressMax == other.ProgressMax &&
        Progress == other.Progress &&
        ProgressPercent == other.ProgressPercent &&
        RemainingText == other.RemainingText &&
        IsUpcomingLesson == other.IsUpcomingLesson;

    public static LessonsLiveActivitySnapshot Loading { get; } = new(
        TimeState.None,
        "loading",
        "ClassIsland",
        "正在加载课程状态…",
        string.Empty,
        "...");
}
