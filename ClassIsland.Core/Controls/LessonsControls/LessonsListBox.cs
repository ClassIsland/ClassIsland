﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Models.Profile;
using unvell.ReoGrid.IO;

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
    public static readonly DependencyProperty ClassPlanProperty = DependencyProperty.Register(
        nameof(ClassPlan), typeof(ClassPlan), typeof(LessonsListBox), new PropertyMetadata(default(ClassPlan)));

    public static readonly DependencyProperty SubjectsProperty = DependencyProperty.Register(
        nameof(Subjects), typeof(ObservableDictionary<string, Subject>), typeof(LessonsListBox), new PropertyMetadata(new ObservableDictionary<string, Subject>()));

    public ObservableDictionary<string, Subject> Subjects
    {
        get { return (ObservableDictionary<string, Subject>)GetValue(SubjectsProperty); }
        set { SetValue(SubjectsProperty, value); }
    }

    /// <summary>
    /// 要显示的课表。
    /// </summary>
    public ClassPlan ClassPlan
    {
        get { return (ClassPlan)GetValue(ClassPlanProperty); }
        set { SetValue(ClassPlanProperty, value); }
    }

    public static readonly DependencyProperty IsLiveUpdatingEnabledProperty = DependencyProperty.Register(
        nameof(IsLiveUpdatingEnabled), typeof(bool), typeof(LessonsListBox), new PropertyMetadata(default(bool)));

    public bool IsLiveUpdatingEnabled
    {
        get { return (bool)GetValue(IsLiveUpdatingEnabledProperty); }
        set { SetValue(IsLiveUpdatingEnabledProperty, value); }
    }

    public static readonly DependencyProperty HighlightChangedClassProperty = DependencyProperty.Register(
        nameof(HighlightChangedClass), typeof(bool), typeof(LessonsListBox), new PropertyMetadata(default(bool)));

    public bool HighlightChangedClass
    {
        get { return (bool)GetValue(HighlightChangedClassProperty); }
        set { SetValue(HighlightChangedClassProperty, value); }
    }

    public static readonly DependencyProperty LessonControlSettingsProperty = DependencyProperty.Register(
        nameof(LessonControlSettings), typeof(ILessonControlSettings), typeof(LessonsListBox), new PropertyMetadata(default(ILessonControlSettings)));

    public ILessonControlSettings LessonControlSettings
    {
        get { return (ILessonControlSettings)GetValue(LessonControlSettingsProperty); }
        set { SetValue(LessonControlSettingsProperty, value); }
    }

    public static readonly DependencyProperty DiscardHidingDefaultProperty = DependencyProperty.Register(
        nameof(DiscardHidingDefault), typeof(bool), typeof(LessonsListBox), new PropertyMetadata(default(bool)/*,
            (o, args) =>
            {
                var control = o as LessonsListBox;
                if (control?.FindResource("LessonsListBoxItemTemplateMultiConverter") is not LessonsListBoxItemTemplateMultiConverter cv)
                    return;

            }*/));

    public bool DiscardHidingDefault
    {
        get { return (bool)GetValue(DiscardHidingDefaultProperty); }
        set { SetValue(DiscardHidingDefaultProperty, value); }
    }

    public static readonly DependencyProperty ShowCurrentTimeLayoutItemOnlyOnClassProperty = DependencyProperty.Register(
        nameof(ShowCurrentTimeLayoutItemOnlyOnClass), typeof(bool), typeof(LessonsListBox), new PropertyMetadata(default(bool)));

    public bool ShowCurrentTimeLayoutItemOnlyOnClass
    {
        get { return (bool)GetValue(ShowCurrentTimeLayoutItemOnlyOnClassProperty); }
        set { SetValue(ShowCurrentTimeLayoutItemOnlyOnClassProperty, value); }
    }

    public static readonly DependencyProperty HideFinishedClassProperty = DependencyProperty.Register(
        nameof(HideFinishedClass), typeof(bool), typeof(LessonsListBox), new PropertyMetadata(default(bool)));

    public bool HideFinishedClass
    {
        get { return (bool)GetValue(HideFinishedClassProperty); }
        set { SetValue(HideFinishedClassProperty, value); }
    }

    static LessonsListBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LessonsListBox), new FrameworkPropertyMetadata(typeof(LessonsListBox)));
    }

    /// <inheritdoc />
    public LessonsListBox()
    {
        //ItemContainerStyleSelector = new LessonsListBoxItemContainerStyleSelector(this);
        //ItemTemplateSelector = new LessonsListBoxItemTemplateSelector(this);
        Loaded += LessonsListBox_Loaded;
        
    }

    private void LessonsListBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (FindResource("LessonsListBoxItemTemplateMultiConverter") is LessonsListBoxItemTemplateMultiConverter cv)
        {
            
        }
    }

    private void CvsOnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is TimeLayoutItem timePoint)
        {
            e.Accepted = timePoint.TimeType != 3;
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
    }
}