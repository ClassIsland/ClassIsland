using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 表示一个日程项目。
/// </summary>
public partial class ScheduleItem : ObservableObject
{
    [ObservableProperty] private Guid _subjectId;

    [ObservableProperty] private TimeSpan _startTime;
    [ObservableProperty] private TimeSpan _endTime;

    [ObservableProperty] private TimeRule _enableRule = new();

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
