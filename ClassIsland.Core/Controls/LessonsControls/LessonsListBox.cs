using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    public static readonly StyledProperty<ObservableOrderedDictionary<string, Subject>> SubjectsProperty =
        AvaloniaProperty.Register<LessonsListBox, ObservableOrderedDictionary<string, Subject>>(
            nameof(Subjects));

    public ObservableOrderedDictionary<string, Subject> Subjects
    {
        get => GetValue(SubjectsProperty);
        set => SetValue(SubjectsProperty, value);
    }

    public static readonly StyledProperty<bool> IsLiveUpdatingEnabledProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(
            nameof(IsLiveUpdatingEnabled));

    public bool IsLiveUpdatingEnabled
    {
        get => GetValue(IsLiveUpdatingEnabledProperty);
        set => SetValue(IsLiveUpdatingEnabledProperty, value);
    }

    public static readonly StyledProperty<bool> HighlightChangedClassProperty =
        AvaloniaProperty.Register<LessonsListBox, bool>(
            nameof(HighlightChangedClass));

    public bool HighlightChangedClass
    {
        get => GetValue(HighlightChangedClassProperty);
        set => SetValue(HighlightChangedClassProperty, value);
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


    /// <inheritdoc />
    public LessonsListBox()
    {
        //ItemContainerStyleSelector = new LessonsListBoxItemContainerStyleSelector(this);
        //ItemTemplateSelector = new LessonsListBoxItemTemplateSelector(this);
    }
}