using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.ScheduleDataGridCell;

public class ScheduleDataGridCellControl : TemplatedControl
{
    public static readonly StyledProperty<ClassInfo> ClassInfoProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, ClassInfo>(
        nameof(ClassInfo), new ClassInfo());

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

    public static readonly AttachedProperty<ObservableDictionary<Guid, Subject>> SubjectsProperty =
        AvaloniaProperty.RegisterAttached<ScheduleDataGridCellControl, Control, ObservableDictionary<Guid, Subject>>("Subjects", inherits: true);

    public static void SetSubjects(Control obj, ObservableDictionary<Guid, Subject> value) => obj.SetValue(SubjectsProperty, value);
    public static ObservableDictionary<Guid, Subject> GetSubjects(Control obj) => obj.GetValue(SubjectsProperty);
    
    
}