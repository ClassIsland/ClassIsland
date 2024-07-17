using System;
using System.Collections.Generic;
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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;

/// <summary>
/// LessonControlExpanded.xaml 的交互逻辑
/// </summary>
public partial class LessonControlExpanded : LessonControlBase, INotifyPropertyChanged
{
    public static readonly DependencyProperty IsLiveUpdatingEnabledProperty = DependencyProperty.Register(
        nameof(IsLiveUpdatingEnabled), typeof(bool), typeof(LessonControlExpanded), new PropertyMetadata(default(bool),
            (o, args) =>
            {
                var control = o as LessonControlExpanded;
                control?.OnIsLiveUpdatePropertyChanged();
            }));

    private long _leftSeconds = 0;
    private long _totalSeconds = 0;
    private long _seconds = 0;
    private int _masterTabIndex = 0;
    private ILessonControlSettings? _settingsSource;

    public bool IsLiveUpdatingEnabled
    {
        get { return (bool)GetValue(IsLiveUpdatingEnabledProperty); }
        set { SetValue(IsLiveUpdatingEnabledProperty, value); }
    }

    public static readonly DependencyProperty CurrentTimeLayoutItemProperty = DependencyProperty.Register(
        nameof(CurrentTimeLayoutItem), typeof(TimeLayoutItem), typeof(LessonControlExpanded), new PropertyMetadata(new TimeLayoutItem()));

    public TimeLayoutItem CurrentTimeLayoutItem
    {
        get { return (TimeLayoutItem)GetValue(CurrentTimeLayoutItemProperty); }
        set { SetValue(CurrentTimeLayoutItemProperty, value); }
    }

    public static readonly DependencyProperty DefaultLessonControlSettingsProperty = DependencyProperty.Register(
        nameof(DefaultLessonControlSettings), typeof(ILessonControlSettings), typeof(LessonControlExpanded), new PropertyMetadata(default(ILessonControlSettings)));

    public ILessonControlSettings? DefaultLessonControlSettings
    {
        get { return (ILessonControlSettings)GetValue(DefaultLessonControlSettingsProperty); }
        set { SetValue(DefaultLessonControlSettingsProperty, value); }
    }

    public static readonly DependencyProperty CurrentClassPlanProperty = DependencyProperty.Register(
        nameof(CurrentClassPlan), typeof(ClassPlan), typeof(LessonControlExpanded), new PropertyMetadata(default(ClassPlan)));

    public ClassPlan CurrentClassPlan
    {
        get { return (ClassPlan)GetValue(CurrentClassPlanProperty); }
        set { SetValue(CurrentClassPlanProperty, value); }
    }

    public ILessonsService? LessonsService { get; set; }

    public long Seconds
    {
        get => _seconds;
        set => SetField(ref _seconds, value);
    }

    public long TotalSeconds
    {
        get => _totalSeconds;
        set => SetField(ref _totalSeconds, value);
    }

    public long LeftSeconds
    {
        get => _leftSeconds;
        set => SetField(ref _leftSeconds, value);
    }

    public int MasterTabIndex
    {
        get => _masterTabIndex;
        set => SetField(ref _masterTabIndex, value);
    }

    private void OnIsLiveUpdatePropertyChanged()
    {
        if (IsLiveUpdatingEnabled)
        {
            LessonsService = IAppHost.GetService<ILessonsService>();
            ExactTimeService = IAppHost.GetService<IExactTimeService>();
            LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        }
        else
        {
            if (LessonsService != null)
            {
                LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
            }
        }
    }

    public ILessonControlSettings? SettingsSource
    {
        get => _settingsSource;
        set => SetField(ref _settingsSource, value);
    }

    private IExactTimeService? ExactTimeService { get; set; }

    /// <inheritdoc />
    public LessonControlExpanded()
    {
        InitializeComponent();
    }

    ~LessonControlExpanded()
    {
        if (LessonsService != null)
        {
            LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
        }
    }

    private Subject CurrentSubject => Subjects.TryGetValue(ClassInfo?.SubjectId ?? "", out var value) ? value : Subject.Breaking;

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        SettingsSource =
            (ILessonControlSettings?)IAttachedSettingsHostService.GetAttachedSettingsByPriority<LessonControlAttachedSettings>(
                new Guid("58e5b69a-764a-472b-bcf7-003b6a8c7fdf"),
                CurrentSubject,
                CurrentTimeLayoutItem,
                CurrentClassPlan,
                ClassInfo?.CurrentTimeLayout) ??
            DefaultLessonControlSettings;

        if (ExactTimeService != null)
        {
            TotalSeconds = (long)CurrentTimeLayoutItem.Last.TotalSeconds;
            Seconds = (long)(ExactTimeService.GetCurrentLocalDateTime().TimeOfDay - CurrentTimeLayoutItem.StartSecond.TimeOfDay).TotalSeconds;
            LeftSeconds = TotalSeconds - Seconds;
        }

        if (SettingsSource != null)
        {
            MasterTabIndex = LeftSeconds <= SettingsSource.CountdownSeconds &&
                             SettingsSource.IsCountdownEnabled &&
                             IsLiveUpdatingEnabled ? 1 : 0;
        }

    }

    #region INotifyPropertyChanged Impletion

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
    

    #endregion

}