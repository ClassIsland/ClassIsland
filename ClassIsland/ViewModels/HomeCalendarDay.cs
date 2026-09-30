using System;

namespace ClassIsland.ViewModels;

public sealed record HomeCalendarDay(DateTime Date, int LessonCount, bool IsToday, bool IsSelected)
{
    public int Day => Date.Day;
    public bool HasLessons => LessonCount > 0;
    public double DotOpacity => HasLessons ? 0.25 + 0.75 * Math.Min(LessonCount, 12) / 12.0 : 0;
    public string Description => $"{Date:yyyy年M月d日 dddd}，{LessonCount} 节课" +
                                 (IsToday ? "，今天" : "") + (IsSelected ? "，已选中" : "");
}
