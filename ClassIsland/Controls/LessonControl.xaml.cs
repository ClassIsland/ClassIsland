using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using ClassIsland.Core;
using ClassIsland.Core.Abstraction.Models;
using ClassIsland.Core.Models.Profile;
using ClassIsland.Models.AttachedSettings;
using ClassIsland.Services;

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

    public SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    private ExactTimeService ExactTimeService { get; } = App.GetService<ExactTimeService>();

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

    public ClassPlan? CurrentClassPlan
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

    public static readonly DependencyProperty LeftSecondsProperty = DependencyProperty.Register(
        nameof(LeftSeconds), typeof(long), typeof(LessonControl), new PropertyMetadata(default(long)));

    public long LeftSeconds
    {
        get { return (long)GetValue(LeftSecondsProperty); }
        set { SetValue(LeftSecondsProperty, value); }
    }

    public static readonly DependencyProperty IsTimerEnabledProperty = DependencyProperty.Register(
        nameof(IsTimerEnabled), typeof(bool), typeof(LessonControl), new PropertyMetadata(true));

    public bool IsTimerEnabled
    {
        get { return (bool)GetValue(IsTimerEnabledProperty); }
        set { SetValue(IsTimerEnabledProperty, value); }
    }

    public static readonly DependencyProperty MasterTabIndexProperty = DependencyProperty.Register(
        nameof(MasterTabIndex), typeof(int), typeof(LessonControl), new PropertyMetadata(default(int)));

    private ILessonControlSettings _settingsSource = App.GetService<SettingsService>().Settings;
    private Subject _currentSubject;

    public int MasterTabIndex
    {
        get { return (int)GetValue(MasterTabIndexProperty); }
        set { SetValue(MasterTabIndexProperty, value); }
    }

    public DispatcherTimer UpdateTimer
    {
        get;
    } = new(DispatcherPriority.Render)
    {
        Interval = TimeSpan.FromMilliseconds(100)
    };

    public ILessonControlSettings SettingsSource
    {
        get => _settingsSource;
        set
        {
            if (Equals(value, _settingsSource)) return;
            _settingsSource = value;
            OnPropertyChanged();
        }
    }

    /* ---------------------------------------------------------------- */

    public Subject CurrentSubject
    {
        get => _currentSubject;
        set
        {
            if (Equals(value, _currentSubject)) return;
            _currentSubject = value;
            OnPropertyChanged();
        }
    }

    private void UpdateCurrentSubject()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (CurrentTimeLayout == null || Subjects == null || CurrentClassPlan == null || Index >= CurrentTimeLayout.Layouts.Count || Index < 0)
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        {
            CurrentSubject = ErrorSubject;
            return;
        }

        //Console.WriteLine($"updated {Index}");
        try
        {
            CurrentSubject = CurrentTimeLayout.Layouts[Index].TimeType switch
            {
                0 => Subjects[CurrentClassPlan.Classes[GetSubjectIndex(Index)].SubjectId],
                1 => BreakingSubject,
                _ => ErrorSubject
            };
        }
        catch
        {
            CurrentSubject = ErrorSubject;
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Update();
        UpdateCurrentSubject();

        switch (e.Property.Name)
        {
            case nameof(CurrentClassPlan):
                if (CurrentClassPlan == null)
                {
                    break;
                }
                CurrentClassPlan.PropertyChanged += CurrentClassPlanOnPropertyChanged;
                CurrentClassPlan.Classes.CollectionChanged += ClassesOnCollectionChanged;
                CurrentClassPlan.ClassesChanged += (sender, args) =>
                {
                    UpdateCurrentSubject();
                    Update();
                };
                //Debug.WriteLine("Add event listener to CurrentClassPlan.PropertyChanged.");
                break;
            case nameof(IsSelected):
                UpdateUpdateTimerStatus();
                break;
        }

        base.OnPropertyChanged(e);
    }

    private void UpdateUpdateTimerStatus()
    {
        if (IsSelected)
        {
            UpdateTimer.Start();
        }
        else
        {
            UpdateTimer.Stop();
        }
    }

    private void ClassesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCurrentSubject();
        Update();
    }

    private void CurrentClassPlanOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentClassPlan.IsActivated))
        {
            return;
        }

        UpdateCurrentSubject();
        Update();
    }

    private void Update()
    {
        if (CurrentClassPlan is null)
        {
            goto final;
        }
        CurrentTimeLayout = CurrentClassPlan.TimeLayout;

        if (Index >= CurrentTimeLayout?.Layouts.Count || CurrentTimeLayout == null || Index < 0)
        {
            goto final;
        }
        CurrentTimeLayoutItem = CurrentTimeLayout.Layouts[Index];

        TotalSeconds = (long)CurrentTimeLayoutItem.Last.TotalSeconds;
        UpdateSeconds();

        final:
        SettingsSource =
            (ILessonControlSettings?)AttachedSettingsHostService.GetAttachedSettingsByPriority<LessonControlAttachedSettings>(
                new Guid("58e5b69a-764a-472b-bcf7-003b6a8c7fdf"),
                CurrentSubject,
                CurrentTimeLayoutItem,
                CurrentClassPlan,
                CurrentTimeLayout) ??
            SettingsService.Settings;
    }

    private void UpdateSeconds()
    {
        Seconds = (long)(ExactTimeService.GetCurrentLocalDateTime().TimeOfDay - CurrentTimeLayoutItem.StartSecond.TimeOfDay).TotalSeconds;
        LeftSeconds = TotalSeconds - Seconds;

        MasterTabIndex = LeftSeconds <= SettingsSource.CountdownSeconds &&
                         SettingsSource.IsCountdownEnabled &&
            IsTimerEnabled ? 1 : 0;
    }

    public LessonControl()
    {
        InitializeComponent();
        UpdateTimer.Tick += UpdateTimerOnTick;
    }

    private void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        if (!IsTimerEnabled)
        {
            return;
        }
        UpdateSeconds();
    }

    protected override void OnInitialized(EventArgs e)
    {
        UpdateUpdateTimerStatus();
        base.OnInitialized(e);
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