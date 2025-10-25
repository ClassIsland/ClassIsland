using System.Reactive.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;


public abstract class LessonControlBase : UserControl
{
    public static readonly StyledProperty<ClassInfo> ClassInfoProperty = AvaloniaProperty.Register<LessonControlBase, ClassInfo>(
        nameof(ClassInfo), new ClassInfo());
    
    public ClassInfo ClassInfo
    {
        get => GetValue(ClassInfoProperty);
        set => SetValue(ClassInfoProperty, value);
    
    }

    public static readonly AttachedProperty<ClassPlan?> ClassPlanProperty =
        AvaloniaProperty.RegisterAttached<LessonControlBase, Control, ClassPlan?>("ClassPlan", inherits: true);

    public static void SetClassPlan(Control obj, ClassPlan? value) => obj.SetValue(ClassPlanProperty, value);
    public static ClassPlan? GetClassPlan(Control obj) => obj.GetValue(ClassPlanProperty);
    

    public static readonly StyledProperty<ObservableDictionary<Guid, Subject>> SubjectsProperty = AvaloniaProperty.Register<LessonControlBase, ObservableDictionary<Guid, Subject>>(
        nameof(Subjects));

    public ObservableDictionary<Guid, Subject> Subjects
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
    
    public static readonly AttachedProperty<ILessonControlSettings> DefaultLessonControlSettingsProperty =
        AvaloniaProperty.RegisterAttached<LessonControlBase, Control, ILessonControlSettings>("DefaultLessonControlSettings", new LessonControlAttachedSettings(), true);

    public static void SetDefaultLessonControlSettings(Control obj, ILessonControlSettings value) => obj.SetValue(DefaultLessonControlSettingsProperty, value);
    public static ILessonControlSettings GetDefaultLessonControlSettings(Control obj) => obj.GetValue(DefaultLessonControlSettingsProperty);

    public LessonControlBase()
    {
        this.GetObservable(ClassPlanProperty).Skip(1).Subscribe(_ => UpdateClassInfo());
        this.GetObservable(CurrentTimeLayoutItemProperty).Skip(1).Subscribe(_ => UpdateClassInfo());
    }

    private void UpdateClassInfo()
    {
        var classPlan = GetClassPlan(this);
        if (classPlan == null || CurrentTimeLayoutItem == null || classPlan.TimeLayout == null)
        {
            return;
        }
        var subjectIndex = GetSubjectIndex(classPlan.TimeLayout.Layouts.IndexOf(CurrentTimeLayoutItem));
        if (subjectIndex >= classPlan.Classes.Count || subjectIndex < 0)
        {
            return;
        }

        ClassInfo = classPlan.Classes[subjectIndex];
        return;

        int GetSubjectIndex(int index)
        {
            if (index == -1)
                return -1;
            var k = classPlan.TimeLayout.Layouts[index];
            var l = classPlan.TimeLayout.Layouts.Where(t => t.TimeType == 0).ToList();
            var i = l.IndexOf(k);
            return i;
        }
    }
}