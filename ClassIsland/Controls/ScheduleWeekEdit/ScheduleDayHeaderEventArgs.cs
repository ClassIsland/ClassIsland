using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClassIsland.Controls.ScheduleWeekEdit;

public sealed class ScheduleDayHeaderEventArgs(RoutedEvent routedEvent, DateOnly date, Control anchor)
    : RoutedEventArgs(routedEvent)
{
    public DateOnly Date { get; } = date;
    public Control Anchor { get; } = anchor;
}
