using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Enums;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using ReactiveUI;

namespace ClassIsland.Controls.Components;

/// <summary>
/// ScheduleComponent.xaml 的交互逻辑
/// </summary>
[MigrateFrom("E7831603-61A0-4180-B51B-54AD75B1A4D3")]  // 课程表（旧）
[ComponentInfo("1DB2017D-E374-4BC6-9D57-0B4ADF03A6B8", "课程表", "\ue751", "显示当前的课程表信息。")]
[PseudoClasses(":show-tomorrow-schedule", ":show-tomorrow-schedule-after-school", ":show-tomorrow-schedule-on-empty", ":show-tomorrow-schedule-always")]
public partial class ScheduleComponent : ComponentBase<LessonControlSettings>, INotifyPropertyChanged
{
    private bool _hideFinishedClass;

    public static readonly DirectProperty<ScheduleComponent, bool> HideFinishedClassProperty = AvaloniaProperty.RegisterDirect<ScheduleComponent, bool>(
        nameof(HideFinishedClass), o => o.HideFinishedClass, (o, v) => o.HideFinishedClass = v);

    public bool HideFinishedClass
    {
        get => _hideFinishedClass;
        set => SetAndRaise(HideFinishedClassProperty, ref _hideFinishedClass, value);
    }

    private bool _showEmptyPlaceholder;

    public static readonly DirectProperty<ScheduleComponent, bool> ShowEmptyPlaceholderProperty = AvaloniaProperty.RegisterDirect<ScheduleComponent, bool>(
        nameof(ShowEmptyPlaceholder), o => o.ShowEmptyPlaceholder, (o, v) => o.ShowEmptyPlaceholder = v);

    public bool ShowEmptyPlaceholder
    {
        get => _showEmptyPlaceholder;
        set => SetAndRaise(ShowEmptyPlaceholderProperty, ref _showEmptyPlaceholder, value);
    }
    
    private bool _showCurrentLessonOnlyOnClass = false;
    private bool _isAfterSchool = false;
    private bool _isDynamicIslandCompactModeActive;
    private bool _isDynamicIslandOnClassModeActive;
    private bool _hasNextLessonCompactBadge;
    private string _nextLessonInitialShort = "?";
    private PrivacyIndicatorState _privacyIndicatorState = PrivacyIndicatorState.None;
    private static readonly IBrush CameraIndicatorBrush = new SolidColorBrush(Colors.LimeGreen);
    private static readonly IBrush MicrophoneIndicatorBrush = new SolidColorBrush(Colors.Orange);
    private static readonly IBrush LocationIndicatorBrush = new SolidColorBrush(Colors.DeepSkyBlue);
    private static readonly IBrush EmptyIndicatorBrush = new SolidColorBrush(Colors.Transparent);
    private ClassPlan? _tomorrowClassPlan;
    private ClassPlan? _tomorrowClassPlan1;
    public ILessonsService LessonsService { get; }

    public SettingsService SettingsService { get; }
    public INotificationHostService NotificationHostService { get; }
    public IPrivacyIndicatorsService PrivacyIndicatorsService { get; }

    public IProfileService ProfileService { get; }
    public IExactTimeService ExactTimeService { get; }

    public static readonly StyledProperty<int> LessonsListBoxSelectedIndexProperty = AvaloniaProperty.Register<ScheduleComponent, int>(
        nameof(LessonsListBoxSelectedIndex));

    private IDisposable? _tomorrowScheduleShowModeObserver;
    private IDisposable? _hideFinishedClassObserver;
    private IDisposable? _showEmptyPlaceholderObserver;
    public int LessonsListBoxSelectedIndex
    {
        get => GetValue(LessonsListBoxSelectedIndexProperty);
        set => SetValue(LessonsListBoxSelectedIndexProperty, value);
    }

    public bool IsAfterSchool
    {
        get => _isAfterSchool;
        set
        {
            if (value == _isAfterSchool) return;
            _isAfterSchool = value;
            OnPropertyChanged();
        }
    }

    public bool ShowCurrentLessonOnlyOnClass
    {
        get => _showCurrentLessonOnlyOnClass;
        set
        {
            if (value == _showCurrentLessonOnlyOnClass) return;
            _showCurrentLessonOnlyOnClass = value;
            OnPropertyChanged();
        }
    }

    public bool IsDynamicIslandCompactModeActive
    {
        get => _isDynamicIslandCompactModeActive;
        set
        {
            if (value == _isDynamicIslandCompactModeActive) return;
            _isDynamicIslandCompactModeActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDynamicIslandRightBadgeVisible));
        }
    }

    public bool HasNextLessonCompactBadge
    {
        get => _hasNextLessonCompactBadge;
        set
        {
            if (value == _hasNextLessonCompactBadge) return;
            _hasNextLessonCompactBadge = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDynamicIslandRightBadgeVisible));
        }
    }

    public string NextLessonInitialShort
    {
        get => _nextLessonInitialShort;
        set
        {
            if (value == _nextLessonInitialShort) return;
            _nextLessonInitialShort = value;
            OnPropertyChanged();
        }
    }

    public PrivacyIndicatorState PrivacyIndicatorState
    {
        get => _privacyIndicatorState;
        set
        {
            if (value == _privacyIndicatorState) return;
            _privacyIndicatorState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PrivacyIndicatorBrush));
            OnPropertyChanged(nameof(IsPrivacyIndicatorVisible));
            OnPropertyChanged(nameof(IsDynamicIslandRightBadgeVisible));
        }
    }

    public IBrush PrivacyIndicatorBrush => PrivacyIndicatorState switch
    {
        PrivacyIndicatorState.Camera => CameraIndicatorBrush,
        PrivacyIndicatorState.Microphone => MicrophoneIndicatorBrush,
        PrivacyIndicatorState.Location => LocationIndicatorBrush,
        _ => EmptyIndicatorBrush
    };

    public bool IsPrivacyIndicatorVisible => PrivacyIndicatorState != PrivacyIndicatorState.None;

    public bool IsDynamicIslandRightBadgeVisible => IsDynamicIslandCompactModeActive &&
                                                    (HasNextLessonCompactBadge || IsPrivacyIndicatorVisible);

    public ClassPlan? TomorrowClassPlan
    {
        get => _tomorrowClassPlan1;
        set
        {
            if (Equals(value, _tomorrowClassPlan1)) return;
            _tomorrowClassPlan1 = value;
            OnPropertyChanged();
        }
    }

    public ScheduleComponent(ILessonsService lessonsService,
        SettingsService settingsService,
        IProfileService profileService,
        IExactTimeService exactTimeService,
        INotificationHostService notificationHostService,
        IPrivacyIndicatorsService privacyIndicatorsService)
    {
        LessonsService = lessonsService;
        SettingsService = settingsService;
        ProfileService = profileService;
        ExactTimeService = exactTimeService;
        NotificationHostService = notificationHostService;
        PrivacyIndicatorsService = privacyIndicatorsService;
        
        AttachedToVisualTree += (_, _) => LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        AttachedToVisualTree += (_, _) => LessonsService.CurrentTimeStateChanged += OnLessonsServiceOnCurrentTimeStateChanged;
        AttachedToVisualTree += (_, _) => LessonsService.PropertyChanged += LessonsServiceOnPropertyChanged;
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += (_, _) => LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
        DetachedFromVisualTree += (_, _) => LessonsService.CurrentTimeStateChanged -= OnLessonsServiceOnCurrentTimeStateChanged;
        DetachedFromVisualTree += (_, _) => LessonsService.PropertyChanged -= LessonsServiceOnPropertyChanged;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
        InitializeComponent();
        CurrentTimeStateChanged();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
    {
        _tomorrowScheduleShowModeObserver?.Dispose();
        _hideFinishedClassObserver?.Dispose();
        _showEmptyPlaceholderObserver?.Dispose();
        PrivacyIndicatorsService.PropertyChanged -= PrivacyIndicatorsServiceOnPropertyChanged;
        
        _tomorrowScheduleShowModeObserver = null;
        _hideFinishedClassObserver = null;
        _showEmptyPlaceholderObserver = null;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
    {
        CurrentTimeStateChanged();
        CheckTomorrowClassShowMode();
        _tomorrowScheduleShowModeObserver ??= Settings
            .ObservableForProperty(x => x.TomorrowScheduleShowMode)
            .Subscribe(_ => CheckTomorrowClassShowMode());
        _hideFinishedClassObserver ??= Settings
            .ObservableForProperty(x => x.HideFinishedClass)
            .Subscribe(_ =>
            {
                HideFinishedClass = Settings.HideFinishedClass;
                UpdateTomorrowVisibility();
            });
        _showEmptyPlaceholderObserver ??= Settings
            .ObservableForProperty(x => x.ShowPlaceholderOnEmptyClassPlan)
            .Subscribe(_ => ShowEmptyPlaceholder = Settings.ShowPlaceholderOnEmptyClassPlan);

        HideFinishedClass = Settings.HideFinishedClass;
        ShowEmptyPlaceholder = Settings.ShowPlaceholderOnEmptyClassPlan;
        MainLessonsListBox.SelectedIndex = LessonsListBoxSelectedIndex;
        PrivacyIndicatorsService.PropertyChanged -= PrivacyIndicatorsServiceOnPropertyChanged;
        PrivacyIndicatorsService.PropertyChanged += PrivacyIndicatorsServiceOnPropertyChanged;
        UpdateDynamicIslandState();
        UpdateNextLessonInitialShort();
        UpdatePrivacyIndicatorState();
        UpdateTomorrowVisibility();
        LessonsServiceOnPostMainTimerTicked(this, EventArgs.Empty);
    }

    private void PrivacyIndicatorsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPrivacyIndicatorsService.CurrentState))
        {
            UpdatePrivacyIndicatorState();
        }
    }

    private void CheckTomorrowClassShowMode()
    {
        PseudoClasses.Set(":show-tomorrow-schedule", Settings.TomorrowScheduleShowMode is 1 or 2 or 3);
        PseudoClasses.Set(":show-tomorrow-schedule-after-school", Settings.TomorrowScheduleShowMode is 1);
        PseudoClasses.Set(":show-tomorrow-schedule-always", Settings.TomorrowScheduleShowMode is 2);
        UpdateTomorrowVisibility();
    }

    private void LessonsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LessonsService.CurrentClassPlan))
        {
            CurrentTimeStateChanged();
        }
    }

    private void OnLessonsServiceOnCurrentTimeStateChanged(object? o, EventArgs eventArgs)
    {
        CurrentTimeStateChanged();
        UpdateDynamicIslandState();
        UpdateNextLessonInitialShort();
        UpdateTomorrowVisibility();
    }

    private void CurrentTimeStateChanged()
    {
        var currentItem = LessonsService.CurrentTimeLayoutItem;
        var hasActiveTimePoint = currentItem != TimeLayoutItem.Empty && currentItem.TimeType is 0 or 1;
        IsAfterSchool = !hasActiveTimePoint &&
                        (LessonsService.CurrentState == TimeState.AfterSchool ||
                         LessonsService.CurrentClassPlan == null);
    }

    public override void OnMigrated(Guid sourceId, object? settings)
    {
        var appSettings = SettingsService.Settings;
        Settings.CountdownSeconds = appSettings.CountdownSeconds;
        Settings.ExtraInfoType = appSettings.ExtraInfoType;
        Settings.IsCountdownEnabled = appSettings.IsCountdownEnabled;
        Settings.ShowExtraInfoOnTimePoint = appSettings.ShowExtraInfoOnTimePoint;
        base.OnMigrated(sourceId, appSettings);
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var settingsSource =
            (ILessonControlSettings?)IAttachedSettingsHostService.GetAttachedSettingsByPriority<LessonControlAttachedSettings>(
                new Guid("58e5b69a-764a-472b-bcf7-003b6a8c7fdf"),
                LessonsService.CurrentSubject,
                LessonsService.CurrentTimeLayoutItem,
                LessonsService.CurrentClassPlan,
                LessonsService.CurrentClassPlan?.TimeLayout) ??
            Settings;
        UpdateDynamicIslandState();
        if (_isDynamicIslandOnClassModeActive)
        {
            ShowCurrentLessonOnlyOnClass = IsDynamicIslandCompactModeActive;
            HideFinishedClass = IsDynamicIslandCompactModeActive;
        }
        else
        {
            ShowCurrentLessonOnlyOnClass = settingsSource.ShowCurrentLessonOnlyOnClass;
            HideFinishedClass = Settings.HideFinishedClass;
        }
        UpdateNextLessonInitialShort();
        UpdatePrivacyIndicatorState();
        TomorrowClassPlan = LessonsService.GetClassPlanByDate(ExactTimeService.GetCurrentLocalDateTime() + TimeSpan.FromDays(1));
        MainLessonsListBox.SelectedIndex = LessonsListBoxSelectedIndex;
        UpdateTomorrowVisibility();
    }

    private void UpdateDynamicIslandState()
    {
        var state = DynamicIslandDisplayStateHelper.GetCurrentState(
            SettingsService.Settings,
            LessonsService,
            ExactTimeService,
            NotificationHostService);
        _isDynamicIslandOnClassModeActive = state.IsOnClassModeActive;
        IsDynamicIslandCompactModeActive = state.IsCompactModeActive;
    }

    private void UpdateNextLessonInitialShort()
    {
        var hasNextLesson = LessonsService.NextClassTimeLayoutItem != TimeLayoutItem.Empty &&
                            !ReferenceEquals(LessonsService.NextClassSubject, Subject.Fallback);
        if (!hasNextLesson)
        {
            HasNextLessonCompactBadge = false;
            NextLessonInitialShort = string.Empty;
            return;
        }

        var initial = LessonsService.NextClassSubject?.Initial;
        if (string.IsNullOrWhiteSpace(initial))
        {
            var name = LessonsService.NextClassSubject?.Name;
            initial = string.IsNullOrWhiteSpace(name) ? null : name.Substring(0, 1);
        }
        if (string.IsNullOrWhiteSpace(initial))
        {
            initial = "?";
        }
        HasNextLessonCompactBadge = true;
        NextLessonInitialShort = initial.Trim().Substring(0, 1);
    }

    private void UpdatePrivacyIndicatorState()
    {
        PrivacyIndicatorState = PrivacyIndicatorsService.CurrentState;
    }

    private void UpdateTomorrowVisibility()
    {
        var showOnEmpty = Settings.TomorrowScheduleShowMode == 3;
        if (!showOnEmpty)
        {
            PseudoClasses.Set(":show-tomorrow-schedule-on-empty", false);
            return;
        }
        var now = ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        var selectedItem = LessonsService.CurrentTimeLayoutItem;
        var classPlan = LessonsService.CurrentClassPlan;
        var hasDisplayable = HasDisplayableItems(classPlan, now, selectedItem);
        PseudoClasses.Set(":show-tomorrow-schedule-on-empty", !hasDisplayable);
    }

    private bool HasDisplayableItems(ClassPlan? classPlan, TimeSpan now, TimeLayoutItem? selectedItem)
    {
        var items = classPlan?.ValidTimeLayoutItems;
        if (items == null || items.Count == 0) return false;
        return items.Any(item => ShouldDisplay(item, now, selectedItem, HideFinishedClass, ShowCurrentLessonOnlyOnClass));
    }

    private static bool ShouldDisplay(TimeLayoutItem item, TimeSpan now, TimeLayoutItem? selectedItem, bool hideFinishedClass, bool showCurrentLessonOnlyOnClass)
    {
        if (item.TimeType is 2 or 3) return false;
        if (hideFinishedClass && item.EndTime < now) return false;
        if (item.IsHideDefault && !Equals(selectedItem, item)) return false;
        if (item.TimeType == 1 && !Equals(selectedItem, item)) return false;
        if (showCurrentLessonOnlyOnClass && selectedItem?.TimeType == 0 && !Equals(selectedItem, item)) return false;
        return true;
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

