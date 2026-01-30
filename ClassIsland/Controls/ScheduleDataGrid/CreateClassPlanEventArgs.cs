using System;

namespace ClassIsland.Controls.ScheduleDataGrid;

public class CreateClassPlanEventArgs : EventArgs
{
    public required DateTime Date { get; init; }
}