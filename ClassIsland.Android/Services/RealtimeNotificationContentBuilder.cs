using System.Globalization;
using ClassIsland.Models;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Android.Services;

internal sealed class RealtimeNotificationContentBuilder
{
    private string? _intervalKey;
    private int _displayOptions;
    private long _rotationStartedAt;

    public static string FormatDuration(int seconds)
    {
        seconds = Math.Max(0, seconds);
        return seconds switch
        {
            >= 3600 => $"{seconds / 3600}h{seconds / 60 % 60}m",
            >= 60 => $"{seconds / 60}min",
            _ => $"{seconds}s"
        };
    }

    public static Subject? GetNextClassSubject(
        ClassPlan? classPlan,
        IDictionary<Guid, Subject> subjects,
        TimeLayoutItem currentItem,
        TimeSpan now)
    {
        // The lessons service's NextClassSubject can still refer to the active lesson.
        var nextItem = classPlan?.ValidTimeLayoutItems.FirstOrDefault(item =>
            item.TimeType == 0 && !ReferenceEquals(item, currentItem) &&
            item.EndTime >= now && item.EndTime > item.StartTime);
        if (nextItem == null)
        {
            return null;
        }

        var nextClass = classPlan!.Classes.FirstOrDefault(info =>
            ReferenceEquals(info.CurrentTimeLayoutItem, nextItem));
        return nextClass != null && subjects.TryGetValue(nextClass.SubjectId, out var subject)
            ? subject
            : Subject.Fallback;
    }

    public RealtimeNotificationContent Build(
        IRealtimeNotificationSettings settings,
        Subject? subject,
        TimeLayoutItem item,
        string remainingTime,
        bool isUpcoming,
        string intervalKey,
        long monotonicMilliseconds)
    {
        subject ??= Subject.Fallback;
        var isBreaking = !isUpcoming && item.TimeType == 1;
        var showSubject = isUpcoming
            ? settings.UpcomingClassRealtimeNotificationShowSubjectName
            : settings.CurrentClassRealtimeNotificationShowSubjectName;
        var showRemaining = isUpcoming
            ? settings.UpcomingClassRealtimeNotificationShowRemainingTime
            : settings.CurrentClassRealtimeNotificationShowRemainingTime;
        var showTimeRange = isUpcoming
            ? settings.UpcomingClassRealtimeNotificationShowTimeRange
            : settings.CurrentClassRealtimeNotificationShowTimeRange;
        var showLocation = isUpcoming
            ? settings.UpcomingClassRealtimeNotificationShowLocation
            : settings.CurrentClassRealtimeNotificationShowLocation;
        var showTeacher = isUpcoming
            ? settings.UpcomingClassRealtimeNotificationShowTeacherName
            : settings.CurrentClassRealtimeNotificationShowTeacherName;
        var displayOptions = (showSubject ? 1 : 0) | (showRemaining ? 2 : 0) |
                             (showTimeRange ? 4 : 0) | (showLocation ? 8 : 0) |
                             (showTeacher ? 16 : 0);
        if (_intervalKey != intervalKey || _displayOptions != displayOptions)
        {
            _intervalKey = intervalKey;
            _displayOptions = displayOptions;
            _rotationStartedAt = monotonicMilliseconds;
        }

        var subjectName = isBreaking
            ? item.BreakNameText
            : ReferenceEquals(subject, Subject.Fallback) || string.IsNullOrWhiteSpace(subject.Name)
                ? "未命名课程"
                : subject.Name.Trim();
        var timeRange = $"{item.StartTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture)}-{item.EndTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture)}";
        var remainingText = isUpcoming ? remainingTime : $"-{remainingTime}";
        List<string> rotatingContent = [];
        if (showRemaining)
        {
            rotatingContent.Add(remainingText);
        }
        if (showTimeRange)
        {
            rotatingContent.Add(timeRange);
        }
        if (!isBreaking && showLocation && !string.IsNullOrWhiteSpace(subject.Location))
        {
            rotatingContent.Add($"@{subject.Location.Trim()}");
        }
        if (!isBreaking && showTeacher && !string.IsNullOrWhiteSpace(subject.TeacherName))
        {
            rotatingContent.Add(subject.TeacherName.Trim());
        }

        var rotatingText = rotatingContent.Count == 0
            ? string.Empty
            : rotatingContent[(int)(Math.Max(0, monotonicMilliseconds - _rotationStartedAt) / 10_000 % rotatingContent.Count)];
        var fixedText = showSubject ? subjectName : string.Empty;
        var summary = string.Join(" ", new[] { fixedText, rotatingText }.Where(x => x.Length > 0));
        var criticalText = rotatingText.Length > 0 ? rotatingText : fixedText;
        if (!isBreaking && criticalText.Length > 0)
        {
            criticalText = $"{(isUpcoming ? "▷" : "▶")}{criticalText}";
        }
        var location = string.IsNullOrWhiteSpace(subject.Location) ? "未设置" : subject.Location.Trim();
        var teacher = string.IsNullOrWhiteSpace(subject.TeacherName) ? "未设置" : subject.TeacherName.Trim();

        return new RealtimeNotificationContent(
            summary,
            criticalText,
            $"{(isUpcoming ? "下一节" : isBreaking ? "课间" : "上课")} · {subjectName}",
            isBreaking
                ? $"{remainingText} {timeRange}"
                : $"{remainingText} {timeRange} @{location} {teacher}");
    }
}

internal sealed record RealtimeNotificationContent(
    string Summary,
    string CriticalText,
    string ExpandedTitle,
    string ExpandedContent);
