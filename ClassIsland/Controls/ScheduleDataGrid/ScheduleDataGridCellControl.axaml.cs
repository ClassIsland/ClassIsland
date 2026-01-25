using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.ScheduleDataGrid;

public class ScheduleDataGridCellControl : TemplatedControl
{
    public static readonly StyledProperty<ClassInfo> ClassInfoProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, ClassInfo>(
        nameof(ClassInfo), ClassInfo.Empty);

    public ClassInfo ClassInfo
    {
        get => GetValue(ClassInfoProperty);
        set => SetValue(ClassInfoProperty, value);
    }

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, bool>(
        nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly StyledProperty<DateTime> DateProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, DateTime>(
        nameof(Date));

    public DateTime Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public static readonly AttachedProperty<ObservableDictionary<Guid, Subject>> SubjectsProperty =
        AvaloniaProperty.RegisterAttached<ScheduleDataGridCellControl, Control, ObservableDictionary<Guid, Subject>>("Subjects", inherits: true);

    public static void SetSubjects(Control obj, ObservableDictionary<Guid, Subject> value) => obj.SetValue(SubjectsProperty, value);
    public static ObservableDictionary<Guid, Subject> GetSubjects(Control obj) => obj.GetValue(SubjectsProperty);

    public static readonly RoutedEvent<ScheduleDataGridSelectionChangedEventArgs>
        ScheduleDataGridSelectionChangedEvent =
            RoutedEvent.Register<ScheduleDataGridCellControl, ScheduleDataGridSelectionChangedEventArgs>(nameof(ScheduleDataGridSelectionChanged), RoutingStrategies.Bubble);

    public event EventHandler<ScheduleDataGridSelectionChangedEventArgs> ScheduleDataGridSelectionChanged
    {
        add => AddHandler(ScheduleDataGridSelectionChangedEvent, value);
        remove => RemoveHandler(ScheduleDataGridSelectionChangedEvent, value);
    }

    private IDisposable? _isSelectedPropertyObserver;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _isSelectedPropertyObserver ??= this.GetObservable(IsSelectedProperty)
            .Skip(1)
            .Subscribe(_ =>
            {
                if (!IsSelected)
                {
                    return;
                }
                RaiseEvent(new ScheduleDataGridSelectionChangedEventArgs(ScheduleDataGridSelectionChangedEvent)
                {
                    ClassInfo = ClassInfo,
                    Date = Date
                });
            });
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _isSelectedPropertyObserver?.Dispose();
        _isSelectedPropertyObserver = null;
    }
}