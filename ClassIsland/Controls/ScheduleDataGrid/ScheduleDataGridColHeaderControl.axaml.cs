using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.ScheduleDataGrid;

public class ScheduleDataGridColHeaderControl : TemplatedControl
{
    public static readonly StyledProperty<ClassPlan> ClassPlanProperty = AvaloniaProperty.Register<ScheduleDataGridColHeaderControl, ClassPlan>(
        nameof(ClassPlan), ScheduleDataGrid.EmptyClassPlan);

    public ClassPlan ClassPlan
    {
        get => GetValue(ClassPlanProperty);
        set => SetValue(ClassPlanProperty, value);
    }

    public static readonly StyledProperty<DateTime> DateProperty = AvaloniaProperty.Register<ScheduleDataGridColHeaderControl, DateTime>(
        nameof(Date));

    public DateTime Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public static readonly StyledProperty<bool> IsClassPlanEmptyProperty = AvaloniaProperty.Register<ScheduleDataGridColHeaderControl, bool>(
        nameof(IsClassPlanEmpty));

    public bool IsClassPlanEmpty
    {
        get => GetValue(IsClassPlanEmptyProperty);
        private set => SetValue(IsClassPlanEmptyProperty, value);
    }

    private IDisposable? _classPlanObserver;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _classPlanObserver ??= this.GetObservable(ClassPlanProperty)
            .Subscribe(x => IsClassPlanEmpty = ClassPlan == ScheduleDataGrid.EmptyClassPlan);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _classPlanObserver?.Dispose();
        _classPlanObserver = null;
    }
}