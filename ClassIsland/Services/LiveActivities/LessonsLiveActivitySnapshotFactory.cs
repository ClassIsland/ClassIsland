using System;
using System.Globalization;
using System.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Services.LiveActivities;

internal sealed class LessonsLiveActivitySnapshotFactory
{
    private ILessonsService LessonsService { get; }
    private IExactTimeService ExactTimeService { get; }

    public LessonsLiveActivitySnapshotFactory(
        ILessonsService lessonsService,
        IExactTimeService exactTimeService)
    {
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
    }

    public LessonsLiveActivitySnapshot Create()
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var absoluteNow = DateTimeOffset.Now;
        return LessonsService.CurrentState switch
        {
            TimeState.OnClass => CreateProgressSnapshot(
                now,
                absoluteNow,
                isClass: true),
            TimeState.Breaking when LessonsService.NextClassTimeLayoutItem != TimeLayoutItem.Empty =>
                CreateUpcomingClassSnapshot(now, absoluteNow),
            TimeState.Breaking => CreateProgressSnapshot(
                now,
                absoluteNow,
                isClass: false),
            TimeState.None when LessonsService.NextClassTimeLayoutItem != TimeLayoutItem.Empty =>
                CreateUpcomingClassSnapshot(now, absoluteNow),
            TimeState.AfterSchool => new LessonsLiveActivitySnapshot(
                TimeState.AfterSchool,
                $"after-school:{now:yyyyMMdd}",
                "已放学",
                "好耶！放学了！(≧∀≦)ゞ",
                string.Empty,
                "已放学"),
            _ => new LessonsLiveActivitySnapshot(
                TimeState.None,
                $"none:{now:yyyyMMdd}",
                "当前无课程",
                "ClassIsland 正在后台运行",
                string.Empty,
                "")
        };
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

    private LessonsLiveActivitySnapshot CreateProgressSnapshot(
        DateTime now,
        DateTimeOffset absoluteNow,
        bool isClass)
    {
        var item = LessonsService.CurrentTimeLayoutItem;
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
            return new LessonsLiveActivitySnapshot(
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

        return new LessonsLiveActivitySnapshot(
            state,
            intervalKey,
            title,
            content,
            FormatTimeRange(item),
            shortTitle,
            totalSeconds,
            progressSeconds,
            progressPercent,
            remainingText,
            GetAbsoluteTime(now, absoluteNow, item.StartTime),
            GetAbsoluteTime(now, absoluteNow, item.EndTime));
    }

    private LessonsLiveActivitySnapshot CreateUpcomingClassSnapshot(
        DateTime now,
        DateTimeOffset absoluteNow)
    {
        var nextItem = LessonsService.NextClassTimeLayoutItem;
        var hasNextClass = !ReferenceEquals(nextItem, TimeLayoutItem.Empty) &&
                           nextItem.EndTime > nextItem.StartTime;
        var nextSubjectName = GetSubjectName(
            LessonsService.NextClassSubject,
            string.Empty);

        var item = LessonsService.CurrentTimeLayoutItem == TimeLayoutItem.Empty
            ? LessonsService.CurrentClassPlan?.TimeLayout?.Layouts
                .Reverse()
                .FirstOrDefault(i =>
                    i.TimeType == 0 &&
                    i.EndTime < now.TimeOfDay)
            : LessonsService.CurrentTimeLayoutItem;
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
            return new LessonsLiveActivitySnapshot(
                TimeState.None,
                CreateIntervalKey(TimeState.None, nextItem, now),
                $"下一节 · {nextSubjectName}",
                $"{FormatTime(nextItem.StartTime)} 开始",
                FormatTimeRange(nextItem),
                $">{nextSubjectName} -{GetDeltaString(now, nextItem.StartTime)}",
                totalSeconds,
                progressSeconds,
                progressPercent,
                remainingText,
                GetAbsoluteTime(now, absoluteNow, startTime),
                GetAbsoluteTime(now, absoluteNow, nextItem.StartTime));
        }

        return new LessonsLiveActivitySnapshot(
            TimeState.None,
            $"none:{now:yyyyMMdd}",
            "当前无课程",
            "ClassIsland 正在后台运行",
            string.Empty,
            "");
    }

    private string CreateNextClassText(bool includeStartTime)
    {
        var nextItem = LessonsService.NextClassTimeLayoutItem;
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

    private static DateTimeOffset GetAbsoluteTime(
        DateTime now,
        DateTimeOffset absoluteNow,
        TimeSpan time)
    {
        var absoluteTime = absoluteNow + (now.Date + time - now);

        // 两次时钟采样间会有亚毫秒抖动。ActivityKit 只需秒级时间，
        // 去掉更细精度可避免同一区间在每次主计时器 tick 时被误判为变化。
        return absoluteTime.AddTicks(
            -(absoluteTime.Ticks % TimeSpan.TicksPerSecond));
    }
}
