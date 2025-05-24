using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;
using ClassIsland.Core.Models.AttachedSettings;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.Components;

/// <summary>
/// ScheduleComponent.xaml 的交互逻辑
/// </summary>
[MigrateFrom("E7831603-61A0-4180-B51B-54AD75B1A4D3")]  // 课程表（旧）
[ComponentInfo("1DB2017D-E374-4BC6-9D57-0B4ADF03A6B8", "课程表", PackIconKind.Schedule, "显示当前的课程表信息。")]
public partial class ScheduleComponent : INotifyPropertyChanged
{
    private bool _showCurrentLessonOnlyOnClass = false;
    private bool _isAfterSchool = false;
    private ClassPlan? _tomorrowClassPlan;
    private ClassPlan? _tomorrowClassPlan1;
    public ILessonsService LessonsService { get; }

    public SettingsService SettingsService { get; }

    public IProfileService ProfileService { get; }
    public IExactTimeService ExactTimeService { get; }

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

    public ScheduleComponent(ILessonsService lessonsService, SettingsService settingsService, IProfileService profileService, IExactTimeService exactTimeService)
    {
        LessonsService = lessonsService;
        SettingsService = settingsService;
        ProfileService = profileService;
        ExactTimeService = exactTimeService;

        Loaded += (_, _) => LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        Loaded += (_, _) => LessonsService.CurrentTimeStateChanged += OnLessonsServiceOnCurrentTimeStateChanged;
        Loaded += (_, _) => LessonsService.PropertyChanged += LessonsServiceOnPropertyChanged;
        Unloaded += (_, _) => LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
        Unloaded += (_, _) => LessonsService.CurrentTimeStateChanged -= OnLessonsServiceOnCurrentTimeStateChanged;
        Unloaded += (_, _) => LessonsService.PropertyChanged -= LessonsServiceOnPropertyChanged;
        InitializeComponent();
        CurrentTimeStateChanged();
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
    }

    private void CurrentTimeStateChanged()
    {
        IsAfterSchool =
            LessonsService.CurrentState == TimeState.AfterSchool ||
            LessonsService.CurrentClassPlan == null;
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
        ShowCurrentLessonOnlyOnClass = settingsSource.ShowCurrentLessonOnlyOnClass;
        TomorrowClassPlan = LessonsService.GetClassPlanByDate(ExactTimeService.GetCurrentLocalDateTime() + TimeSpan.FromDays(1));
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
