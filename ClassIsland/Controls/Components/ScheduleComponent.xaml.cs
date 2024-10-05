using ClassIsland.Core.Abstractions.Controls;
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


    private bool CheckIsAfterSchool()
    {
        if (LessonsService.CurrentState != TimeState.None)
        {
            return false;
        }

        return LessonsService is { IsClassPlanLoaded: true, CurrentClassPlan: not null } &&
               ExactTimeService.GetCurrentLocalDateTime().TimeOfDay >= LessonsService.CurrentClassPlan?.TimeLayout
                   .Layouts.LastOrDefault()?.EndSecond.TimeOfDay;
    }

    public ScheduleComponent(ILessonsService lessonsService, SettingsService settingsService, IProfileService profileService, IExactTimeService exactTimeService)
    {
        LessonsService = lessonsService;
        SettingsService = settingsService;
        ProfileService = profileService;
        ExactTimeService = exactTimeService;
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        LessonsService.CurrentTimeStateChanged += LessonsServiceOnCurrentTimeStateChanged;
        LessonsService.PropertyChanged += LessonsServiceOnPropertyChanged;
        IsAfterSchool = CheckIsAfterSchool();
        InitializeComponent();
    }

    private void LessonsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LessonsService.CurrentClassPlan) or nameof(LessonsService.IsClassPlanLoaded))
        {
            IsAfterSchool = CheckIsAfterSchool();
        }
    }


    private void LessonsServiceOnCurrentTimeStateChanged(object? sender, EventArgs e)
    {
        IsAfterSchool = CheckIsAfterSchool();
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
        //IsAfterSchool = CheckIsAfterSchool();
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