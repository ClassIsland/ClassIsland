using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Shared;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Models.Profile;
using ReactiveUI;

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
    private ILessonControlSettings? _settingsSource = new LessonControlAttachedSettings();
    private Subject _displayingSubject = Subject.Empty;
    private bool _attachedToVisualTree = false;
    private string _detailedInfoText = "";

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

    public string DetailedInfoText
    {
        get => _detailedInfoText;
        set => SetField(ref _detailedInfoText, value);
    }

    private void OnIsLiveUpdatePropertyChanged()
    {
        UpdateLiveUpdateSettings();
    }

    private void UpdateLiveUpdateSettings()
    {
        if (IsLiveUpdatingEnabled && _attachedToVisualTree)
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
        this.GetObservable(ClassPlanProperty).Subscribe(_ => UpdateSubject());
        this.GetObservable(ClassInfoProperty).Subscribe(_ =>
        {
            ClassInfo.ObservableForProperty(x => x.SubjectId)
                .Subscribe(_ => UpdateSubject());
        });
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }


    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _attachedToVisualTree = true;
        UpdateLiveUpdateSettings();
    }
    
    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _attachedToVisualTree = false;
        UpdateLiveUpdateSettings();
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
                GetClassPlan(this),
                ClassInfo?.CurrentTimeLayout) ??
            GetDefaultLessonControlSettings(this);

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
            if (ExtraInfo4ShowSeconds)
            {
                DetailIndex = 5;
            }
            else
            {
                DetailIndex = IsLiveUpdatingEnabled ? SettingsSource.ExtraInfoType : 0;
            }
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
