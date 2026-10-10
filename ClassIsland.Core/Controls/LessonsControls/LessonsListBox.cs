using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;

/// <summary>
/// 用于显示课表的列表控件。 
/// </summary>
public class LessonsListBox : ListBox
{
    /// <summary>
    /// Gets or sets the direction in which lessons are arranged.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<LessonsListBox, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static string MinimizedLessonControlResourceKey { get; } = "MinimizedLessonControl";
    public static string ExpandedLessonControlResourceKey { get; } = "ExpandedLessonControl";
    public static string SeparatorLessonControlResourceKey { get; } = "SeparatorLessonControl";
    public static string NormalItemContainerStyleKey { get; } = "NormalItemContainer";
    public static string BlankItemContainerStyleKey { get; } = "BlankItemContainer";


    /// <summary>
    /// 属性<see cref="ClassPlan"/>的依赖属性。
    /// </summary>
    public static readonly StyledProperty<ClassPlan> ClassPlanProperty =
        AvaloniaProperty.Register<LessonsListBox, ClassPlan>(
            nameof(ClassPlan));

    public ClassPlan ClassPlan
    {
        get => GetValue(ClassPlanProperty);
        set => SetValue(ClassPlanProperty, value);
    }

    public static readonly AttachedProperty<ObservableOrderedDictionary<Guid, Subject>> SubjectsProperty =
        AvaloniaProperty.RegisterAttached<LessonsListBox, Control, ObservableOrderedDictionary<Guid, Subject>>("Subjects", inherits:true);

    public static void SetSubjects(Control obj, ObservableOrderedDictionary<Guid, Subject> value) => obj.SetValue(SubjectsProperty, value);
    public static ObservableOrderedDictionary<Guid, Subject> GetSubjects(Control obj) => obj.GetValue(SubjectsProperty);

    public static readonly AttachedProperty<bool> IsLiveUpdatingEnabledProperty =
        AvaloniaProperty.RegisterAttached<LessonsListBox, Control, bool>("IsLiveUpdatingEnabled", false, true);

    public static void SetIsLiveUpdatingEnabled(Control obj, bool value) => obj.SetValue(IsLiveUpdatingEnabledProperty, value);
    public static bool GetIsLiveUpdatingEnabled(Control obj) => obj.GetValue(IsLiveUpdatingEnabledProperty);
    

    public static readonly StyledProperty<bool> HighlightChangedClassProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(
            nameof(HighlightChangedClass));

    public bool HighlightChangedClass
    {
        get => GetValue(HighlightChangedClassProperty);
        set => SetValue(HighlightChangedClassProperty, value);
    }

    /// <summary>
    /// 是否在横版折叠状态下用科目图标替代简称。
    /// </summary>
    public static readonly StyledProperty<bool> ShowSubjectIconWhenMinimizedProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(nameof(ShowSubjectIconWhenMinimized), false);

    public bool ShowSubjectIconWhenMinimized
    {
        get => GetValue(ShowSubjectIconWhenMinimizedProperty);
        set => SetValue(ShowSubjectIconWhenMinimizedProperty, value);
    }

    /// <summary>
    /// 是否在横版展开状态下显示科目图标。
    /// </summary>
    public static readonly StyledProperty<bool> ShowSubjectIconWhenExpandedProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(nameof(ShowSubjectIconWhenExpanded), false);

    public static readonly StyledProperty<double> SubjectIconScaleProperty = AvaloniaProperty.Register<LessonsListBox, double>(
        nameof(SubjectIconScale), 1.0);

    public double SubjectIconScale
    {
        get => GetValue(SubjectIconScaleProperty);
        set => SetValue(SubjectIconScaleProperty, value);
    }

    public bool ShowSubjectIconWhenExpanded
    {
        get => GetValue(ShowSubjectIconWhenExpandedProperty);
        set => SetValue(ShowSubjectIconWhenExpandedProperty, value);
    }

    public static readonly StyledProperty<ILessonControlSettings> LessonControlSettingsProperty =
        AvaloniaProperty.Register<LessonsListBox, ILessonControlSettings>(
            nameof(LessonControlSettings));

    public ILessonControlSettings LessonControlSettings
    {
        get => GetValue(LessonControlSettingsProperty);
        set => SetValue(LessonControlSettingsProperty, value);
    }

    public static readonly StyledProperty<bool> DiscardHidingDefaultProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(
            nameof(DiscardHidingDefault));

    public bool DiscardHidingDefault
    {
        get => GetValue(DiscardHidingDefaultProperty);
        set => SetValue(DiscardHidingDefaultProperty, value);
    }

    public static readonly StyledProperty<bool> ShowCurrentTimeLayoutItemOnlyOnClassProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(
            nameof(ShowCurrentTimeLayoutItemOnlyOnClass));

    public bool ShowCurrentTimeLayoutItemOnlyOnClass
    {
        get => GetValue(ShowCurrentTimeLayoutItemOnlyOnClassProperty);
        set => SetValue(ShowCurrentTimeLayoutItemOnlyOnClassProperty, value);
    }

    public static readonly StyledProperty<bool> HideFinishedClassProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(
            nameof(HideFinishedClass));

    public bool HideFinishedClass
    {
        get => GetValue(HideFinishedClassProperty);
        set => SetValue(HideFinishedClassProperty, value);
    }

    public static readonly StyledProperty<bool> FadeCompletedClassesProperty = AvaloniaProperty.Register<LessonsListBox, bool>(
        nameof(FadeCompletedClasses));

    public bool FadeCompletedClasses
    {
        get => GetValue(FadeCompletedClassesProperty);
        set => SetValue(FadeCompletedClassesProperty, value);
    }


    /// <inheritdoc />
    public LessonsListBox()
    {
        //ItemContainerStyleSelector = new LessonsListBoxItemContainerStyleSelector(this);
        //ItemTemplateSelector = new LessonsListBoxItemTemplateSelector(this);
    }
}
