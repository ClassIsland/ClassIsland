using System;

namespace ClassIsland.Models;

/// <summary>
/// A dated projection of a course; multiple occurrences retain the same course ID.
/// </summary>
public sealed record ScheduleWeekOccurrence(Guid ScheduleItemId, DateOnly Date, string SubjectName,
    TimeSpan StartTime, TimeSpan EndTime);
