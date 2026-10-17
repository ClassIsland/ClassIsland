using CommunityToolkit.Mvvm.ComponentModel;

using System.Text.Json.Serialization;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 表示一个课程在指定日期替换的课程。
/// </summary>
public sealed class ScheduleItemReplacementTarget
{
    /// <summary>
    /// 初始化一个课程替换目标。
    /// </summary>
    /// <param name="scheduleItemId">被替换课程 ID。</param>
    /// <param name="date">替换发生的日期。</param>
    public ScheduleItemReplacementTarget(Guid scheduleItemId, DateTime date)
    {
        ScheduleItemId = scheduleItemId;
        Date = date;
    }

    /// <summary>
    /// 被替换课程 ID。
    /// </summary>
    public Guid ScheduleItemId { get; }

    /// <summary>
    /// 替换发生的日期。
    /// </summary>
    public DateTime Date { get; }
}

/// <summary>
/// 表示一个日程项目。
/// </summary>
public partial class ScheduleItem : ObservableObject
{
    [ObservableProperty] private Guid _subjectId;
    [ObservableProperty] private string _location = "";

    [ObservableProperty] private TimeSpan _startTime;
    [ObservableProperty] private TimeSpan _endTime;

    [ObservableProperty] private bool _isEnabled = true;

    [ObservableProperty] private TimeRule _enableRule = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReplacementTarget))]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private ScheduleItemReplacementTarget? _replacementTarget;

    /// <summary>
    /// 当前课程是否替换了另一个课程。
    /// </summary>
    [JsonIgnore]
    public bool HasReplacementTarget => ReplacementTarget != null;

    partial void OnStartTimeChanged(TimeSpan value)
    {
        if (EndTime < value)
        {
            EndTime = value;
        }
    }

    partial void OnEndTimeChanged(TimeSpan value)
    {
        if (StartTime > value)
        {
            StartTime = value;
        }
    }
}
