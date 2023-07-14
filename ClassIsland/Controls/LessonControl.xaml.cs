using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Threading;
using ClassIsland.Models;

namespace ClassIsland.Controls;

/// <summary>
/// LessonControl.xaml 的交互逻辑
/// </summary>
public partial class LessonControl : UserControl, INotifyPropertyChanged
{
    public static Subject BreakingSubject
    {
        get;
    } = new()
    {
        Initial = "休",
        Name = "课间休息"
    };
    public static Subject ErrorSubject
    {
        get;
    } = new()
    {
        Initial = ":(",
        Name = ":( 出错了"
    };

    public static readonly DependencyProperty CurrentTimeLayoutItemProperty = DependencyProperty.Register(
        nameof(CurrentTimeLayoutItem), typeof(TimeLayoutItem), typeof(LessonControl), new PropertyMetadata(default(TimeLayoutItem)));

    public TimeLayoutItem CurrentTimeLayoutItem
    {
        get => (TimeLayoutItem)GetValue(CurrentTimeLayoutItemProperty);
        set => SetValue(CurrentTimeLayoutItemProperty, value);
    }

    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(
        nameof(Index), typeof(int), typeof(LessonControl), new PropertyMetadata(default(int)));

    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public static readonly DependencyProperty SubjectsProperty = DependencyProperty.Register(
        nameof(Subjects), typeof(ObservableDictionary<string, Subject>), typeof(LessonControl), new PropertyMetadata(default(ObservableCollection<Subject>)));

    public ObservableDictionary<string, Subject> Subjects
    {
        get => (ObservableDictionary<string, Subject>)GetValue(SubjectsProperty);
        set => SetValue(SubjectsProperty, value);
    }

    public static readonly DependencyProperty CurrentTimeLayoutProperty = DependencyProperty.Register(
        nameof(CurrentTimeLayout), typeof(TimeLayout), typeof(LessonControl), new PropertyMetadata(default(TimeLayout)));

    public TimeLayout CurrentTimeLayout
    {
        get => (TimeLayout)GetValue(CurrentTimeLayoutProperty);
        set => SetValue(CurrentTimeLayoutProperty, value);
    }

    public static readonly DependencyProperty CurrentClassPlanProperty = DependencyProperty.Register(
        nameof(CurrentClassPlan), typeof(ClassPlan), typeof(LessonControl), new PropertyMetadata(default(ClassPlan)));

    public ClassPlan CurrentClassPlan
    {
        get => (ClassPlan)GetValue(CurrentClassPlanProperty);
        set => SetValue(CurrentClassPlanProperty, value);
    }

    private int GetSubjectIndex(int index)
    {
        var k = CurrentTimeLayout.Layouts[index];
        var i = (from t in CurrentTimeLayout.Layouts where t.TimeType==0 select t).ToList().IndexOf(k);
        return i;
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(LessonControl), new PropertyMetadata(false));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(
        nameof(Seconds), typeof(long), typeof(LessonControl), new PropertyMetadata(default(long)));

    public long Seconds
    {
        get => (long)GetValue(SecondsProperty);
        set => SetValue(SecondsProperty, value);
    }

    public static readonly DependencyProperty TotalSecondsProperty = DependencyProperty.Register(
        nameof(TotalSeconds), typeof(long), typeof(LessonControl), new PropertyMetadata(default(long)));

    public long TotalSeconds
    {
        get => (long)GetValue(TotalSecondsProperty);
        set => SetValue(TotalSecondsProperty, value);
    }

    public DispatcherTimer UpdateTimer
    {
        get;
    } = new(DispatcherPriority.Render)
    {
        Interval = TimeSpan.FromMilliseconds(100)
    };

    /* ---------------------------------------------------------------- */

    public Subject CurrentSubject
    {
        get
        {
            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (CurrentTimeLayout == null || Subjects == null || CurrentClassPlan == null)
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            {
                return ErrorSubject;
            }
            return CurrentTimeLayout.Layouts[Index].TimeType switch
            {
                0 => Subjects[CurrentClassPlan.Classes[GetSubjectIndex(Index)].SubjectId],
                1 => BreakingSubject,
                _ => ErrorSubject
            };
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Update();
        OnPropertyChanged(nameof(CurrentSubject));
        base.OnPropertyChanged(e);
    }

    private void Update()
    {
        if (CurrentClassPlan is null)
        {
            return;
        }
        CurrentTimeLayout = CurrentClassPlan.TimeLayout;
        CurrentTimeLayoutItem = CurrentTimeLayout.Layouts[Index];

        TotalSeconds = (long)CurrentTimeLayoutItem.Last.TotalSeconds;
        Seconds = (long)(DateTime.Now.TimeOfDay - CurrentTimeLayoutItem.StartSecond.TimeOfDay).TotalSeconds;
    }

    public LessonControl()
    {
        InitializeComponent();
        UpdateTimer.Tick += UpdateTimerOnTick;
        UpdateTimer.Start();
    }

    private void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        Seconds = (long)(DateTime.Now.TimeOfDay - CurrentTimeLayoutItem.StartSecond.TimeOfDay).TotalSeconds;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}