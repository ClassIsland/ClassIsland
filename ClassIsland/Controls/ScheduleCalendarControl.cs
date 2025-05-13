using System;
using System.Windows.Controls;

namespace ClassIsland.Controls;

public class ScheduleCalendarControl : Calendar
{
    public event EventHandler? ScheduleUpdated;

    public void UpdateSchedule()
    {
        ScheduleUpdated?.Invoke(this, EventArgs.Empty);
    }
}