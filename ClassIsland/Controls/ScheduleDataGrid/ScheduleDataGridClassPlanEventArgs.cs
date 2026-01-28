using Avalonia.Interactivity;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.ScheduleDataGrid;

public class ScheduleDataGridClassPlanEventArgs(RoutedEvent e) : RoutedEventArgs(e)
{
    public required ClassPlan ClassPlan { get; init; }
}