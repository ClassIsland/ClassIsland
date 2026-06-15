using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

public enum ReminderFrequency
{
    Once = 0,
    Daily = 1,
    Weekly = 2,
    Yearly = 3
}

[Flags]
public enum ReminderWeekDays
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

/// <summary>
/// 日程/提醒条目模型，支持单次/每日/每周/每年重复
/// </summary>
public class Reminder : ObservableRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private string _title = "新日程";
    public string Title
    {
        get => _title;
        set
        {
            if (value == _title) return;
            if (string.IsNullOrWhiteSpace(value)) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    public string Message { get; set; } = "";

    /// <summary>
    /// 下次触发时间（用于快速展示/排序）。
    /// </summary>
    public DateTime Time { get; set; } = DateTime.Now;

    /// <summary>
    /// 重复类型（无/每天/每周/每年）
    /// </summary>
    public ReminderFrequency Frequency { get; set; } = ReminderFrequency.Once;

    /// <summary>
    /// 周重复时选中的星期几
    /// </summary>
    public ReminderWeekDays WeekDays { get; set; } = ReminderWeekDays.Weekdays;

    /// <summary>
    /// 年度重复时使用的月（1-12）和日（1-31）。如果为0则使用Time的日期部分。
    /// </summary>
    public int YearMonth { get; set; } = 0;
    public int YearDay { get; set; } = 0;

    /// <summary>
    /// 时间部分（在重复或单次提醒中使用）。
    /// </summary>
    public TimeSpan TimeOfDay { get; set; } = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

    /// <summary>
    /// 起始与结束日期（可选），用于限定重复生效区间。
    /// </summary>
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 通知所有显示相关属性已更改，强制 UI 刷新列表绑定。
    /// </summary>
    public void NotifyPropertiesChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(Time));
        OnPropertyChanged(nameof(TimeOfDay));
        OnPropertyChanged(nameof(Frequency));
        OnPropertyChanged(nameof(WeekDays));
        OnPropertyChanged(nameof(StartDate));
        OnPropertyChanged(nameof(EndDate));
        OnPropertyChanged(nameof(IsEnabled));
    }

    /// <summary>
    /// 计算从给定时间点起的下一个发生时间（若无则返回 null）。
    /// </summary>
    public DateTime? GetNextOccurrence(DateTime from)
    {
        if (!IsEnabled) return null;

        return Frequency switch
        {
            ReminderFrequency.Once => NextOnce(from),
            ReminderFrequency.Daily => NextDaily(from),
            ReminderFrequency.Weekly => NextWeekly(from),
            ReminderFrequency.Yearly => NextYearly(from),
            _ => null
        };
    }

    private bool InRange(DateTime dt)
    {
        if (StartDate.HasValue && dt.Date < StartDate.Value.Date) return false;
        if (EndDate.HasValue && dt.Date > EndDate.Value.Date) return false;
        return true;
    }

    private DateTime? NextOnce(DateTime from)
    {
        var occ = Time;
        if (occ >= from && InRange(occ)) return occ;
        return null;
    }

    private DateTime? NextDaily(DateTime from)
    {
        var candidate = new DateTime(from.Year, from.Month, from.Day).Add(TimeOfDay);
        if (candidate < from) candidate = candidate.AddDays(1);
        return InRange(candidate) ? candidate : null;
    }

    private DateTime? NextWeekly(DateTime from)
    {
        var start = from.Date;
        for (int i = 0; i < 14; i++)
        {
            var candidateDate = start.AddDays(i);
            var flag = DayOfWeekToFlag(candidateDate.DayOfWeek);
            if (WeekDays.HasFlag(flag))
            {
                var candidate = candidateDate.Add(TimeOfDay);
                if (candidate >= from && InRange(candidate)) return candidate;
            }
        }
        return null;
    }

    private DateTime? NextYearly(DateTime from)
    {
        int month = YearMonth > 0 ? YearMonth : Time.Month;
        int day = YearDay > 0 ? YearDay : Time.Day;
        var year = from.Year;
        DateTime candidate;
        try
        {
            candidate = new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month))).Add(TimeOfDay);
        }
        catch
        {
            candidate = new DateTime(year, 1, 1).AddYears(1).Add(TimeOfDay);
        }
        if (candidate < from) candidate = candidate.AddYears(1);
        return InRange(candidate) ? candidate : null;
    }

    private static ReminderWeekDays DayOfWeekToFlag(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Sunday => ReminderWeekDays.Sunday,
        DayOfWeek.Monday => ReminderWeekDays.Monday,
        DayOfWeek.Tuesday => ReminderWeekDays.Tuesday,
        DayOfWeek.Wednesday => ReminderWeekDays.Wednesday,
        DayOfWeek.Thursday => ReminderWeekDays.Thursday,
        DayOfWeek.Friday => ReminderWeekDays.Friday,
        DayOfWeek.Saturday => ReminderWeekDays.Saturday,
        _ => ReminderWeekDays.None
    };

    /// <summary>
    /// 将内部存储的下次时间提前到下一次发生（若 Repeat==Once 则禁用）。
    /// </summary>
    public void AdvanceNextOccurrence()
    {
        var now = DateTime.Now.AddSeconds(1);
        var next = GetNextOccurrence(now);
        if (next.HasValue)
        {
            Time = next.Value;
        }
        else
        {
            IsEnabled = false;
        }
    }
}
