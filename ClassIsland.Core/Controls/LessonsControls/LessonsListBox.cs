using System;
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

    public static readonly DependencyProperty AdditionInfoTypeProperty = DependencyProperty.Register(
        nameof(AdditionInfoType), typeof(int), typeof(LessonsListBox), new PropertyMetadata(default(int)));

    public int AdditionInfoType
    {
        get { return (int)GetValue(AdditionInfoTypeProperty); }
        set { SetValue(AdditionInfoTypeProperty, value); }
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
        
    }
}