
using System;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

public class ScheduleCalendarControl : Calendar
{
    public event EventHandler? ScheduleUpdated;

    public static readonly AttachedProperty<SyncDictionaryList<Guid, ClassPlan>> ClassPlanListProperty =
        AvaloniaProperty.RegisterAttached<ScheduleCalendarControl, Control, SyncDictionaryList<Guid, ClassPlan>>("ClassPlanList", inherits: true);

    private static void SetClassPlanList(Control obj, SyncDictionaryList<Guid, ClassPlan> value) => obj.SetValue(ClassPlanListProperty, value);
    public static SyncDictionaryList<Guid, ClassPlan> GetClassPlanList(Control obj) => obj.GetValue(ClassPlanListProperty);

    public void UpdateSchedule()
    {
        ScheduleUpdated?.Invoke(this, EventArgs.Empty);
    }

    public ScheduleCalendarControl()
    {
        SetClassPlanList(this, new SyncDictionaryList<Guid, ClassPlan>(IAppHost.GetService<IProfileService>().Profile.ClassPlans, Guid.NewGuid));
    }
}
