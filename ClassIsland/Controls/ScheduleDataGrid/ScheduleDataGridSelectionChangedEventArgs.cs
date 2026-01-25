using System;
using Avalonia.Interactivity;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.ScheduleDataGrid;

public class ScheduleDataGridSelectionChangedEventArgs(RoutedEvent e) : RoutedEventArgs(e)
{
    public required ClassInfo ClassInfo { get; init; }
    
    public required DateTime Date { get; init; }
}