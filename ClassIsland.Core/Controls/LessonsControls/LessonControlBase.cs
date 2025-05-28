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

    public static readonly StyledProperty<ObservableDictionary<string, Subject>> SubjectsProperty = AvaloniaProperty.Register<LessonControlBase, ObservableDictionary<string, Subject>>(
        nameof(Subjects));
    
    public ObservableDictionary<string, Subject> Subjects
    {
        get => GetValue(SubjectsProperty);
        set => SetValue(SubjectsProperty, value);
    
    }
}