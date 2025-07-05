using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Reactive;
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
    public static readonly StyledProperty<bool> IsLiveUpdatingEnabledProperty = AvaloniaProperty.Register<LessonControlExpanded, bool>(
        nameof(IsLiveUpdatingEnabled));

    public bool IsLiveUpdatingEnabled
    {
        get => GetValue(IsLiveUpdatingEnabledProperty);
        set => SetValue(IsLiveUpdatingEnabledProperty, value);
    }

    private long _leftSeconds = 0;
    private long _totalSeconds = 0;
    private long _seconds = 0;
    private int _masterTabIndex = 0;
    private bool _extraInfo4ShowSeconds = false;
    private ILessonControlSettings? _settingsSource;
    private Subject _displayingSubject = Subject.Empty;

    public static readonly StyledProperty<ILessonControlSettings> DefaultLessonControlSettingsProperty = AvaloniaProperty.Register<LessonControlExpanded, ILessonControlSettings>(
        nameof(DefaultLessonControlSettings), new LessonControlAttachedSettings());
    public ILessonControlSettings DefaultLessonControlSettings
    {
        get => GetValue(DefaultLessonControlSettingsProperty);
        set => SetValue(DefaultLessonControlSettingsProperty, value);
    
    }

    public static readonly StyledProperty<ClassPlan> CurrentClassPlanProperty = AvaloniaProperty.Register<LessonControlExpanded, ClassPlan>(
        nameof(CurrentClassPlan));
    public ClassPlan CurrentClassPlan
    {
        get => GetValue(CurrentClassPlanProperty);
        set => SetValue(CurrentClassPlanProperty, value);
    
    }

    public static readonly StyledProperty<int> DetailIndexProperty = AvaloniaProperty.Register<LessonControlExpanded, int>(
        nameof(DetailIndex));

    public int DetailIndex
    {
        get => GetValue(DetailIndexProperty);
        set => SetValue(DetailIndexProperty, value);
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

    public bool ExtraInfo4ShowSeconds
    {
        get => _extraInfo4ShowSeconds;
        set => SetField(ref _extraInfo4ShowSeconds, value);
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

    public Subject DisplayingSubject
    {
        get => _displayingSubject;
        set => SetField(ref _displayingSubject, value);
    }

    private void UpdateSubject()
    {
        if (CurrentTimeLayoutItem == null)
        {
            return;
        }

        DisplayingSubject = CurrentTimeLayoutItem.TimeType == 1 ? Subject.Breaking : CurrentSubject;
    }

    private IExactTimeService? ExactTimeService { get; set; }

    /// <inheritdoc />
    public LessonControlExpanded()
    {
        InitializeComponent();

        this.GetObservable(IsLiveUpdatingEnabledProperty)
            .Subscribe(new AnonymousObserver<bool>(_ => OnIsLiveUpdatePropertyChanged()));
        this.GetObservable(CurrentTimeLayoutItemProperty).Subscribe(_ => UpdateSubject());
    }

    ~LessonControlExpanded()
    {
        if (LessonsService != null)
        {
            LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
        }
    }

    private Subject CurrentSubject => Subjects?.TryGetValue(ClassInfo?.SubjectId ?? Guid.Empty, out var value) == true ? value : Subject.Breaking;

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

        if (ExactTimeService != null && CurrentTimeLayoutItem != null)
        {
            TotalSeconds = (long)CurrentTimeLayoutItem.Last.TotalSeconds;
            Seconds = (long)(ExactTimeService.GetCurrentLocalDateTime().TimeOfDay - CurrentTimeLayoutItem.StartTime).TotalSeconds;
            LeftSeconds = TotalSeconds - Seconds;
        }

        if (SettingsSource != null)
        {
            MasterTabIndex = LeftSeconds <= SettingsSource.CountdownSeconds &&
                             SettingsSource.IsCountdownEnabled &&
                             IsLiveUpdatingEnabled ? 1 : 0;
            ExtraInfo4ShowSeconds = SettingsSource.ExtraInfoType == 4 &&
                                    LeftSeconds <= SettingsSource.ExtraInfo4ShowSecondsSeconds;
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
