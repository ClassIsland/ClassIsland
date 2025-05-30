using System.Reactive.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;


public abstract class LessonControlBase : UserControl
{
    public static readonly StyledProperty<ClassInfo> ClassInfoProperty = AvaloniaProperty.Register<LessonControlBase, ClassInfo>(
        nameof(ClassInfo));
    
    public ClassInfo ClassInfo
    {
        get => GetValue(ClassInfoProperty);
        set => SetValue(ClassInfoProperty, value);
    
    }

    public static readonly StyledProperty<ClassPlan?> ClassPlanProperty = AvaloniaProperty.Register<LessonControlBase, ClassPlan?>(
        nameof(ClassPlan));

    public ClassPlan? ClassPlan
    {
        get => GetValue(ClassPlanProperty);
        set => SetValue(ClassPlanProperty, value);
    }

    public static readonly StyledProperty<ObservableDictionary<string, Subject>> SubjectsProperty = AvaloniaProperty.Register<LessonControlBase, ObservableDictionary<string, Subject>>(
        nameof(Subjects));

    public ObservableDictionary<string, Subject> Subjects
    {
        get => GetValue(SubjectsProperty);
        set => SetValue(SubjectsProperty, value);
    }

    public static readonly StyledProperty<TimeLayoutItem?> CurrentTimeLayoutItemProperty = AvaloniaProperty.Register<LessonControlBase, TimeLayoutItem?>(
        nameof(CurrentTimeLayoutItem));

    public TimeLayoutItem? CurrentTimeLayoutItem
    {
        get => GetValue(CurrentTimeLayoutItemProperty);
        set => SetValue(CurrentTimeLayoutItemProperty, value);
    }

    public LessonControlBase()
    {
        this.GetObservable(ClassPlanProperty).Skip(1).Subscribe(_ => UpdateClassInfo());
        this.GetObservable(CurrentTimeLayoutItemProperty).Skip(1).Subscribe(_ => UpdateClassInfo());
    }

    private void UpdateClassInfo()
    {
        if (ClassPlan == null || CurrentTimeLayoutItem == null)
        {
            return;
        }
        var subjectIndex = GetSubjectIndex(ClassPlan.TimeLayout.Layouts.IndexOf(CurrentTimeLayoutItem));
        if (subjectIndex >= ClassPlan.Classes.Count || subjectIndex < 0)
        {
            return;
        }

        ClassInfo = ClassPlan.Classes[subjectIndex];
        return;

        int GetSubjectIndex(int index)
        {
            if (index == -1)
                return -1;
            var k = ClassPlan.TimeLayout.Layouts[index];
            var l = ClassPlan.TimeLayout.Layouts.Where(t => t.TimeType == 0).ToList();
            var i = l.IndexOf(k);
            return i;
        }
    }
}