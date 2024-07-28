using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Sentry;

namespace ClassIsland.Services;

public class LessonsService : ObservableRecipient, ILessonsService
{
    private ClassPlan? _currentClassPlan;
    private int? _currentSelectedIndex;
    private Subject _nextSubject = Subject.Empty;
    private TimeLayoutItem _nextTimeLayoutItem = new();
    private TimeLayoutItem _nextBreakingLayoutItem = new();
    private TimeSpan _onClassLeftTime = TimeSpan.Zero;
    private TimeState _currentStatus = TimeState.None;
    private TimeState _currentOverlayStatus = TimeState.None;
    private TimeLayoutItem _currentTimeLayoutItem = new();
    private Subject? _currentSubject;
    private bool _isClassPlanEnabled = true;
    private TimeState _currentOverlayEventStatus = TimeState.None;
    private bool _isClassPlanLoaded = false;
    private bool _isLessonConfirmed = false;
    private TimeSpan _onBreakingTimeLeftTime = TimeSpan.Zero;
    private TimeLayoutItem _nextClassTimeLayoutItem = new();

    private DispatcherTimer MainTimer
    {
        get;
    } = new(DispatcherPriority.Render)
    {
        Interval = TimeSpan.FromMilliseconds(50)
    };

    public event EventHandler? PreMainTimerTicked;
    public event EventHandler? PostMainTimerTicked;
    public bool IsTimerRunning => MainTimer.IsEnabled;

    public ClassPlan? CurrentClassPlan
    {
        get => _currentClassPlan;
        set => SetProperty(ref _currentClassPlan, value);
    }

    public int? CurrentSelectedIndex
    {
        get => _currentSelectedIndex;
        set => SetProperty(ref _currentSelectedIndex, value);
    }

    public Subject NextClassSubject
    {
        get => _nextSubject;
        set => SetProperty(ref _nextSubject, value);
    }

    //public TimeLayoutItem NextTimeLayoutItem
    //{
    //    get => _nextTimeLayoutItem;
    //    set => SetProperty(ref _nextTimeLayoutItem, value);
    //}

    public TimeLayoutItem NextBreakingTimeLayoutItem
    {
        get => _nextBreakingLayoutItem;
        set => SetProperty(ref _nextBreakingLayoutItem, value);
    }

    public TimeLayoutItem NextClassTimeLayoutItem
    {
        get => _nextClassTimeLayoutItem;
        set => SetProperty(ref _nextClassTimeLayoutItem, value);
    }

    public TimeSpan OnClassLeftTime
    {
        get => _onClassLeftTime;
        set => SetProperty(ref _onClassLeftTime, value);
    }

    public TimeState CurrentState
    {
        get => _currentStatus;
        set => SetProperty(ref _currentStatus, value);
    }

    public TimeState CurrentOverlayStatus
    {
        get => _currentOverlayStatus;
        set => SetProperty(ref _currentOverlayStatus, value);
    }

    public TimeLayoutItem CurrentTimeLayoutItem
    {
        get => _currentTimeLayoutItem;
        set => SetProperty(ref _currentTimeLayoutItem, value);
    }

    public Subject? CurrentSubject
    {
        get => _currentSubject;
        set => SetProperty(ref _currentSubject, value);
    }

    public bool IsClassPlanEnabled
    {
        get => _isClassPlanEnabled;
        set => SetProperty(ref _isClassPlanEnabled, value);
    }

    public bool IsClassPlanLoaded
    {
        get => _isClassPlanLoaded;
        set => SetProperty(ref _isClassPlanLoaded, value);
    }

    public bool IsLessonConfirmed
    {
        get => _isLessonConfirmed;
        set => SetProperty(ref _isLessonConfirmed, value);
    }

    public TimeSpan OnBreakingTimeLeftTime
    {
        get => _onBreakingTimeLeftTime;
        set => SetProperty(ref _onBreakingTimeLeftTime, value);
    }

    public event EventHandler? OnClass;
    public event EventHandler? OnBreakingTime;
    public event EventHandler? OnAfterSchool;
    public event EventHandler? CurrentTimeStateChanged;
    public void DebugTriggerOnClass()
    {
        OnClass?.Invoke(this, EventArgs.Empty);
    }

    public void DebugTriggerOnBreakingTime()
    {
        OnBreakingTime?.Invoke(this, EventArgs.Empty);
    }

    public void DebugTriggerOnAfterSchool()
    {
        OnAfterSchool?.Invoke(this, EventArgs.Empty);
    }

    public void DebugTriggerOnStateChanged()
    {
        CurrentTimeStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private SettingsService SettingsService { get; }

    private IProfileService ProfileService { get; }

    private ILogger<LessonsService> Logger { get; }

    private IExactTimeService ExactTimeService { get; }

    private Profile Profile => ProfileService.Profile;

    private Settings Settings => SettingsService.Settings;

    public LessonsService(SettingsService settingsService, IProfileService profileService, ILogger<LessonsService> logger, IExactTimeService exactTimeService)
    {
        MainTimer.Tick += MainTimerOnTick;
        SettingsService = settingsService;
        ProfileService = profileService;
        Logger = logger;
        ExactTimeService = exactTimeService;

        StartMainTimer();
    }

    private void MainTimerOnTick(object? sender, EventArgs e)
    {
        PreMainTimerTicked?.Invoke(this, EventArgs.Empty);
        ProcessLessons();
        PostMainTimerTicked?.Invoke(this, EventArgs.Empty);
    }

    private void ProcessLessons()
    {
        LoadCurrentClassPlan();
        // Deactivate
        foreach (var i in Profile.TimeLayouts.Where(i => !i.Value.IsActivatedManually))
        {
            i.Value.IsActivated = false;
        }
        foreach (var i in Profile.ClassPlans)
        {
            i.Value.IsActivated = false;
        }

        if (CurrentClassPlan?.TimeLayout is null)
        {
            CurrentState = TimeState.None;
            CurrentOverlayStatus = TimeState.None;
            CurrentOverlayEventStatus = TimeState.None;
            IsClassPlanLoaded = false;
            CurrentSelectedIndex = -1;
            return;
        }
        IsClassPlanLoaded = true;
        // Activate selected item
        CurrentClassPlan.IsActivated = true;
        if (!Settings.ExpAllowEditingActivatedTimeLayout)
        {
            CurrentClassPlan.TimeLayout.IsActivated = true;
        }

        var isLessonConfirmed = false;
        // 更新选择
        var currentLayout = CurrentClassPlan.TimeLayout.Layouts;
        var currentLayoutItem = currentLayout.FirstOrDefault(i =>
            i.StartSecond.TimeOfDay <= ExactTimeService.GetCurrentLocalDateTime().TimeOfDay &&
            i.EndSecond.TimeOfDay >= ExactTimeService.GetCurrentLocalDateTime().TimeOfDay &&
            i.TimeType != 2);
        if (currentLayoutItem != null)
        {
            CurrentSelectedIndex = currentLayout.IndexOf(currentLayoutItem);
            CurrentTimeLayoutItem = currentLayoutItem;
            IsLessonConfirmed = isLessonConfirmed = true;
            if (CurrentTimeLayoutItem.TimeType == 0)
            {
                var i0 = GetSubjectIndex(currentLayout.IndexOf(currentLayoutItem));
                CurrentSubject = Profile.Subjects[CurrentClassPlan.Classes[i0].SubjectId];
            }
            else
            {
                CurrentSubject = null;
            }
        }

        //var isBreaking = false;
        if (!isLessonConfirmed)
        {
            CurrentSelectedIndex = -1;
            CurrentState = TimeState.None;
            IsLessonConfirmed = false;
        }
        // 获取下节课信息
        else if (CurrentSelectedIndex + 1 < currentLayout.Count && CurrentSelectedIndex is not null)
        {
            var nextClassTimeLayoutItem = currentLayout.FirstOrDefault(i =>
                    currentLayout.IndexOf(i) > CurrentSelectedIndex && i.TimeType == 0);
            var nextBreakingTimeLayoutItem = currentLayout.FirstOrDefault(i =>
                    currentLayout.IndexOf(i) > CurrentSelectedIndex && i.TimeType == 1);
            if (nextClassTimeLayoutItem != null)
            {
                NextClassTimeLayoutItem = nextClassTimeLayoutItem;
                var i0 = GetSubjectIndex(currentLayout.IndexOf(nextClassTimeLayoutItem));
                var index = CurrentClassPlan.Classes[i0].SubjectId;
                Profile.Subjects.TryGetValue(index, out var subject);
                NextClassSubject = subject ?? Subject.Empty;
            }

            if (nextBreakingTimeLayoutItem != null)
            {
                NextBreakingTimeLayoutItem = NextBreakingTimeLayoutItem = nextBreakingTimeLayoutItem;
            }
        }

        var tClassDelta = NextClassTimeLayoutItem.StartSecond.TimeOfDay - ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        OnClassLeftTime = tClassDelta;
        OnBreakingTimeLeftTime = NextBreakingTimeLayoutItem.StartSecond.TimeOfDay - ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        // 获取状态信息
        if (CurrentSelectedIndex == null || CurrentSelectedIndex == -1)
        {
            CurrentState = TimeState.None;
        }
        else if (CurrentTimeLayoutItem.TimeType == 0)
        {
            CurrentState = TimeState.OnClass;
        }
        else if (CurrentTimeLayoutItem.TimeType == 1)
        {
            CurrentState = TimeState.Breaking;
        }

        // 发出状态变更事件
        if (CurrentState != CurrentOverlayEventStatus)
        {
            CurrentTimeStateChanged?.Invoke(this, EventArgs.Empty);
        }
        switch (CurrentState)
        {
            // 下课事件
            case TimeState.Breaking when CurrentOverlayEventStatus != TimeState.Breaking:
                Logger.LogInformation("发出下课事件。");
                OnBreakingTime?.Invoke(this, EventArgs.Empty);
                CurrentOverlayEventStatus = TimeState.Breaking;
                break;
            // 上课事件
            case TimeState.OnClass when CurrentOverlayEventStatus != TimeState.OnClass:
                Logger.LogInformation("发出上课事件。");
                OnClass?.Invoke(this, EventArgs.Empty);
                CurrentOverlayEventStatus = TimeState.OnClass;
                break;
            case TimeState.None:
                break;
            case TimeState.PrepareOnClass:
                break;
            default:
                break;
        }

        CurrentOverlayEventStatus = CurrentState;
    }

    private int GetSubjectIndex(int index)
    {
        var k = CurrentClassPlan?.TimeLayout.Layouts[index];
        var l = (from t in CurrentClassPlan?.TimeLayout.Layouts where t.TimeType == 0 select t).ToList();
        var i = l.IndexOf(k);
        return i;
    }

    public TimeState CurrentOverlayEventStatus
    {
        get => _currentOverlayEventStatus;
        set => SetProperty(ref _currentOverlayEventStatus, value);
    }

    private void LoadCurrentClassPlan()
    {
        ProfileService.Profile.RefreshTimeLayouts();
        if (Profile.TempClassPlanSetupTime.Date < ExactTimeService.GetCurrentLocalDateTime().Date)  // 清除过期临时课表
        {
            Profile.TempClassPlanId = null;
        }
        ProfileService.CleanExpiredTempClassPlan(); // 清除过期的临时层

        // 清除过期的临时课表群
        ProfileService.ClearExpiredTempClassPlanGroup();

        // 检测是否启用课表加载
        if (!IsClassPlanEnabled)
        {   
            CurrentClassPlan = null;
            return;
        }
        // 加载临时层
        if (Profile.IsOverlayClassPlanEnabled &&
            Profile.OverlayClassPlanId != null &&
            Profile.ClassPlans.TryGetValue(Profile.OverlayClassPlanId, out var overlay))
        {
            CurrentClassPlan = overlay;
            return;
        }
        // 加载临时课表
        if (Profile.TempClassPlanId != null &&
            Profile.ClassPlans.TryGetValue(Profile.TempClassPlanId, out var tempClassPlan))
        {
            CurrentClassPlan = tempClassPlan;
            return;
        }
        // 加载课表
        var a = Profile.ClassPlans
            .Where(x =>
            {
                var group = x.Value.AssociatedGroup;
                var matchGlobal = group == ClassPlanGroup.GlobalGroupGuid.ToString();
                var matchDefault = group == Profile.SelectedClassPlanGroupId;
                if (Profile is not { IsTempClassPlanGroupEnabled: true, TempClassPlanGroupId: not null })
                    return matchDefault || matchGlobal;
                var matchTemp = group == Profile.TempClassPlanGroupId;
                return Profile.TempClassPlanGroupType switch
                {
                    TempClassPlanGroupType.Inherit => matchDefault || matchTemp || matchGlobal,
                    TempClassPlanGroupType.Override => matchTemp || matchGlobal,
                    _ => matchDefault || matchGlobal
                };
            })
            .OrderByDescending(x =>
            {
                var group = x.Value.AssociatedGroup;
                if (group == Profile.TempClassPlanGroupId) return 3;
                if (group == Profile.SelectedClassPlanGroupId) return 2;
                if (group == ClassPlanGroup.GlobalGroupGuid.ToString()) return 1;
                return 0;
            })
            .Where(p => CheckClassPlan(p.Value))
            .Select(p => p.Value);
        CurrentClassPlan = a.FirstOrDefault();


    }

    public bool CheckClassPlan(ClassPlan plan)
    {
        if (plan.IsOverlay || !plan.IsEnabled)
            return false;

        if (plan.TimeRule.WeekDay != (int)ExactTimeService.GetCurrentLocalDateTime().DayOfWeek)
        {
            return false;
        }

        if (plan.AssociatedGroup != ClassPlanGroup.GlobalGroupGuid.ToString() &&
            plan.AssociatedGroup != Profile.SelectedClassPlanGroupId &&
            plan.AssociatedGroup != Profile.TempClassPlanGroupId)
        {
            return false;
        }

        var dd = ExactTimeService.GetCurrentLocalDateTime().Date - Settings.SingleWeekStartTime.Date;
        var dw = Math.Floor(dd.TotalDays / 7) + 1;
        var w = (int)dw % 2;
        switch (plan.TimeRule.WeekCountDiv)
        {
            case 1 when w != 1:
                return false;
            case 2 when w != 0:
                return false;
            default:
                return true;
        }
    }

    public void StartMainTimer()
    {
        MainTimer.Start();
    }

    public void StopMainTimer()
    {
        MainTimer.Stop();
    }
}