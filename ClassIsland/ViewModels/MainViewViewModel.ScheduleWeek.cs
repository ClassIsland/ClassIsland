using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class MainViewViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScheduleWeekCaption))]
    private DateOnly _scheduleWeekStart;
    [ObservableProperty] private DateOnly? _scheduleWeekSelectedDate;
    [ObservableProperty] private TimeSpan? _scheduleWeekSelectedTime;
    private DateOnly? _lastCurrentScheduleWeek;

    public string ScheduleWeekCaption
    {
        get
        {
            // Match the profile editor's week numbering, which uses Saturday as its reference.
            var referenceDate = ScheduleWeekStart.AddDays(5).ToDateTime(TimeOnly.MinValue);
            var week = (int)Math.Ceiling((referenceDate - _calendarSettings.Settings.SingleWeekStartTime).TotalDays / 7);
            return $"第 {week} 周";
        }
    }

    public void RefreshScheduleWeek()
    {
        var today = DateOnly.FromDateTime(_exactTimeService.GetCurrentLocalDateTime());
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        if (_lastCurrentScheduleWeek == null || ScheduleWeekStart == _lastCurrentScheduleWeek)
            SetScheduleWeek(monday.DayNumber);
        _lastCurrentScheduleWeek = monday;
        OnPropertyChanged(nameof(ScheduleWeekCaption));
    }

    public void MoveScheduleWeek(int weeks) => SetScheduleWeek(ScheduleWeekStart.DayNumber + weeks * 7L);

    public void GoToCurrentScheduleWeek()
    {
        var today = DateOnly.FromDateTime(_exactTimeService.GetCurrentLocalDateTime());
        SetScheduleWeek(today.DayNumber - (((int)today.DayOfWeek + 6) % 7));
    }

    private void SetScheduleWeek(long day)
    {
        if (day < 0 || day > DateOnly.MaxValue.DayNumber - 6) return;
        ScheduleWeekStart = DateOnly.FromDayNumber((int)day);
    }

    partial void OnScheduleWeekStartChanged(DateOnly value)
    {
        ScheduleWeekSelectedDate = null;
        ScheduleWeekSelectedTime = null;
    }
}
