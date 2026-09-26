using System;
using Avalonia.Interactivity;

namespace ClassIsland.Controls.ScheduleWeekEdit;

public sealed class ScheduleWeekEditEventArgs(RoutedEvent routedEvent) : RoutedEventArgs(routedEvent)
{
    public Guid? ScheduleItemId { get; init; }
    public DateOnly OriginalDate { get; init; }
    public DateOnly Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
}
