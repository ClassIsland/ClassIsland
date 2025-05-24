using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Models.Rules;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class LessonsService : ObservableRecipient, ILessonsService
{
    private ClassPlan? _currentClassPlan;
    private int _currentSelectedIndex = -1;
    private Subject _nextSubject = Subject.Empty;
    private TimeLayoutItem _nextBreakingLayoutItem = TimeLayoutItem.Empty;
    private TimeSpan _onClassLeftTime = TimeSpan.Zero;
    private TimeState _currentStatus = TimeState.None;
    private TimeState _currentOverlayStatus = TimeState.None;
    private TimeLayoutItem _currentTimeLayoutItem = TimeLayoutItem.Empty;
    private Subject? _currentSubject;
    private bool _isClassPlanEnabled = true;
    private TimeState _currentOverlayEventStatus = TimeState.None;
    private bool _isClassPlanLoaded = false;
    private bool _isLessonConfirmed = false;
    private TimeSpan _onBreakingTimeLeftTime = TimeSpan.Zero;
    private TimeLayoutItem _nextClassTimeLayoutItem = TimeLayoutItem.Empty;
    private ObservableCollection<int> _multiWeekRotation = [0, 0, 1, 1, 1];

    private static readonly ObservableCollection<int> DefaultMultiWeekRotation = [0, 0, 1, 1, 1];

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

    public int CurrentSelectedIndex
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
    public ObservableCollection<int> MultiWeekRotation
    {
        get => _multiWeekRotation;
        set => SetProperty(ref _multiWeekRotation, value);
    }

    public ClassPlan? GetClassPlanByDate(DateTime date) => GetClassPlanByDate(date, out _);

    public ClassPlan? GetClassPlanByDate(DateTime date, out string? guid)
    {
        guid = null;
        // 加载临时层（弃用）
        // 现在临时层使用预定临时课表的加载逻辑。
        //if (Profile is { IsOverlayClassPlanEnabled: true, OverlayClassPlanId: not null } &&
        //    Profile.ClassPlans.TryGetValue(Profile.OverlayClassPlanId, out var overlay) &&
        //    overlay.OverlaySetupTime.Date >= date.Date)
        //{
        //    return overlay;
        //}
        // 加载预定的临时课表
        if (Profile.OrderedSchedules.TryGetValue(date.Date, out var orderedScheduleInfo)
            && Profile.ClassPlans.TryGetValue(orderedScheduleInfo.ClassPlanId, out var orderedClassPlan)
            && (!orderedClassPlan.IsOverlay || Profile.IsOverlayClassPlanEnabled))
        {
            guid = orderedScheduleInfo.ClassPlanId;
            return orderedClassPlan;
        }
        // 加载临时课表
        if (Profile.TempClassPlanId != null &&
            Profile.ClassPlans.TryGetValue(Profile.TempClassPlanId, out var tempClassPlan) &&
            Profile.TempClassPlanSetupTime.Date >= date.Date)
        {
            guid = Profile.TempClassPlanId;
            return tempClassPlan;
        }
        // 加载课表
        var a = Profile.ClassPlans
            .Where(x =>
            {
                var group = x.Value.AssociatedGroup;
                var matchGlobal = new Guid(group) == ClassPlanGroup.GlobalGroupGuid;
                var matchDefault = group == Profile.SelectedClassPlanGroupId;
                if (Profile is not { IsTempClassPlanGroupEnabled: true, TempClassPlanGroupId: not null } 
                    || Profile.TempClassPlanGroupExpireTime.Date < date.Date)
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
            .Where(p => CheckClassPlan(p.Value, date))
            .Select(p => p);
        var classPlanKvp = a.FirstOrDefault();
        guid = classPlanKvp.Key;
        return classPlanKvp.Value;
    }

    public event EventHandler? OnClass;
    public event EventHandler? OnBreakingTime;
    public event EventHandler? OnAfterSchool;
    public event EventHandler? CurrentTimeStateChanged;

    public void DebugTriggerOnClass() => OnClass?.Invoke(this, EventArgs.Empty);
    public void DebugTriggerOnBreakingTime() => OnBreakingTime?.Invoke(this, EventArgs.Empty);
    public void DebugTriggerOnAfterSchool() => OnAfterSchool?.Invoke(this, EventArgs.Empty);
    public void DebugTriggerOnStateChanged() => CurrentTimeStateChanged?.Invoke(this, EventArgs.Empty);

    private SettingsService SettingsService { get; }
    private IProfileService ProfileService { get; }
    private ILogger<LessonsService> Logger { get; }
    private IExactTimeService ExactTimeService { get; }
    public IRulesetService RulesetService { get; }
    public IIpcService IpcService { get; }

    private Profile Profile => ProfileService.Profile;
    private Settings Settings => SettingsService.Settings;

    public LessonsService(SettingsService settingsService, IProfileService profileService, ILogger<LessonsService> logger, IExactTimeService exactTimeService, IRulesetService rulesetService, IIpcService ipcService)
    {
        MainTimer.Tick += MainTimerOnTick;
        SettingsService = settingsService;
        ProfileService = profileService;
        Logger = logger;
        ExactTimeService = exactTimeService;
        RulesetService = rulesetService;
        IpcService = ipcService;

        IpcService.IpcProvider.CreateIpcJoint<IPublicLessonsService>(this);
        RulesetService.RegisterRuleHandler("classisland.lessons.timeState", TimeStateHandler);
        RulesetService.RegisterRuleHandler("classisland.lessons.currentSubject", CurrentSubjectHandler);
        RulesetService.RegisterRuleHandler("classisland.lessons.nextSubject", NextSubjectHandler);
        RulesetService.RegisterRuleHandler("classisland.lessons.previousSubject", PreviousSubjectHandler);
        CurrentTimeStateChanged += (sender, args) => RulesetService.NotifyStatusChanged();
        PropertyChanged += OnPropertyChanged;
        PropertyChanging += OnPropertyChanging;


        CurrentTimeStateChanged += async (_, _) =>
        {
            Logger.LogInformation("发出时间状态改变事件。");
            await IpcService.BroadcastNotificationAsync(IpcRoutedNotifyIds.CurrentTimeStateChangedNotifyId);
        };
        OnClass += async (_, _) =>
        {
            Logger.LogInformation("发出上课事件。");
            await IpcService.BroadcastNotificationAsync(IpcRoutedNotifyIds.OnClassNotifyId);
        };
        OnBreakingTime += async (_, _) =>
        {
            Logger.LogInformation("发出下课事件。");
            await IpcService.BroadcastNotificationAsync(IpcRoutedNotifyIds.OnBreakingTimeNotifyId);
        };
        OnAfterSchool += async (_, _) =>
        {
            Logger.LogInformation("发出放学事件。");
            await IpcService.BroadcastNotificationAsync(IpcRoutedNotifyIds.OnAfterSchoolNotifyId);
        };

        ProcessLessons();  // 防止在课程服务初始化后因没有更新课表获取到错误的信息
        StartMainTimer();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(CurrentSubject))
        {
            RulesetService.NotifyStatusChanged();
        }

        if (args.PropertyName == nameof(CurrentClassPlan) && CurrentClassPlan != null)
        {
            CurrentClassPlan.ClassesChanged += CurrentClassPlanOnClassesChanged;
            CurrentClassPlan.RefreshIsChangedClass();
        }
    }

    private void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentClassPlan) && CurrentClassPlan != null)
        {
            CurrentClassPlan.ClassesChanged -= CurrentClassPlanOnClassesChanged;
        }
    }

    private void CurrentClassPlanOnClassesChanged(object? sender, EventArgs e)
    {
        
    }

    private bool CurrentSubjectHandler(object? settings)
    {
        if (settings is not CurrentSubjectRuleSettings s)
        {
            return false;
        }

        if (!ProfileService.Profile.Subjects.TryGetValue(s.SubjectId, out var subject))
        {
            return false;
        }

        return CurrentSubject == subject;
    }

    private bool PreviousSubjectHandler(object? settings)
    {
        if (settings is not CurrentSubjectRuleSettings s)
        {
            return false;
        }

        if (!ProfileService.Profile.Subjects.TryGetValue(s.SubjectId, out var subject))
        {
            return false;
        }

        var now = ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        var layout = CurrentClassPlan?.TimeLayout;
        if (layout == null)
        {
            return false;
        }
        var prevClassTimeItem = layout.Layouts
            .Reverse()
            .FirstOrDefault(i =>
                i.TimeType == 0 &&
                i.EndSecond.TimeOfDay < now);
        if (prevClassTimeItem == null)
        {
            return false;
        }
        var i0 = GetClassIndex(layout.Layouts.IndexOf(prevClassTimeItem));
        if (i0 >= 0 && CurrentClassPlan?.Classes.Count > i0 &&
            Profile.Subjects.TryGetValue(CurrentClassPlan.Classes[i0].SubjectId, out var prevSubject))
        {
            return prevSubject == subject;
        }

        return false;
    }

    private bool NextSubjectHandler(object? settings)
    {
        if (settings is not CurrentSubjectRuleSettings s)
        {
            return false;
        }

        if (!ProfileService.Profile.Subjects.TryGetValue(s.SubjectId, out var subject))
        {
            return false;
        }

        return NextClassSubject == subject;
    }

    private bool TimeStateHandler(object? settings)
    {
        if (settings is not TimeStateRuleSettings s)
        {
            return false;
        }

        return CurrentState == s.State ||
               (CurrentState == TimeState.AfterSchool && s.State == TimeState.None);
    }

    private void MainTimerOnTick(object? sender, EventArgs e)
    {
        using var scope = Logger.BeginScope("MainTimerTicked");
        using (Logger.BeginScope("PreTicked"))
        {
            PreMainTimerTicked?.Invoke(this, EventArgs.Empty);
        }
        using (Logger.BeginScope("ProcessLessons"))
        {
            ProcessLessons();
        }
        using (Logger.BeginScope("PostTicked"))
        {
            PostMainTimerTicked?.Invoke(this, EventArgs.Empty);
        }
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

        // 预定所有需要更新的信息
        int? currentSelectedIndex = null;
        TimeState? currentState = null;
        Subject? currentSubject = null;
        Subject? nextClassSubject = null;
        TimeLayoutItem? currentTimeLayoutItem = null;
        TimeLayoutItem? nextClassTimeLayoutItem = null;
        TimeLayoutItem? nextBreakingTimeLayoutItem = null;
        TimeSpan? onClassLeftTime = null;
        TimeSpan? onBreakingTimeLeftTime = null;
        bool? isLessonConfirmed = null;
        bool? isClassPlanLoaded = null;

        var layout = CurrentClassPlan?.TimeLayout?.Layouts;
        if (layout == null) // 当前没有课表时，跳过获取信息
        {
            goto final;
        }

        // 开始获取信息
        isClassPlanLoaded = true;
        // Activate selected item
        CurrentClassPlan!.IsActivated = true;
        CurrentClassPlan.TimeLayout.IsActivated = true;

        var now = ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        var validTimeLayoutItems = CurrentClassPlan.ValidTimeLayoutItems;

        // 获取当前时间点信息
        currentTimeLayoutItem = validTimeLayoutItems.FirstOrDefault(i =>
            i.TimeType is 0 or 1 &&
            i.StartSecond.TimeOfDay <= now &&
            i.EndSecond.TimeOfDay >= now);
        if (currentTimeLayoutItem != null)
        {
            currentSelectedIndex = layout.IndexOf(currentTimeLayoutItem);
            if (currentTimeLayoutItem.TimeType == 0)
            {
                var i0 = GetClassIndex((int)currentSelectedIndex);
                if (i0 >= 0 && CurrentClassPlan.Classes.Count > i0 &&
                    Profile.Subjects.TryGetValue(CurrentClassPlan.Classes[i0].SubjectId, out var subject))
                {
                    currentSubject = subject;
                    currentState = TimeState.OnClass;
                }
            }
            else if (currentTimeLayoutItem.TimeType == 1)
            {
                currentSubject = Subject.Breaking;
                currentSubject.Name = currentTimeLayoutItem.BreakNameText;
                currentState = TimeState.Breaking;
            }
            isLessonConfirmed = true;
        }

        // 获取下节时间点信息
        nextClassTimeLayoutItem = validTimeLayoutItems.FirstOrDefault(i =>
            i.TimeType == 0 &&
            i.EndSecond.TimeOfDay >= now);
        if (nextClassTimeLayoutItem != null)
        {
            var i0 = GetClassIndex(layout.IndexOf(nextClassTimeLayoutItem));
            if (i0 >= 0 && CurrentClassPlan.Classes.Count > i0 &&
                Profile.Subjects.TryGetValue(CurrentClassPlan.Classes[i0].SubjectId, out var subject))
                nextClassSubject = subject;
        }
        nextBreakingTimeLayoutItem = validTimeLayoutItems.FirstOrDefault(i =>
            i.TimeType == 1 &&
            i.EndSecond.TimeOfDay >= now);

        // 获取剩余时间信息
        if (currentState == TimeState.OnClass)
            onBreakingTimeLeftTime = nextBreakingTimeLayoutItem?.StartSecond.TimeOfDay - now;
        else
            onClassLeftTime = nextClassTimeLayoutItem?.StartSecond.TimeOfDay - now;

        if (nextClassTimeLayoutItem == null &&
            nextBreakingTimeLayoutItem == null)
            currentState = TimeState.AfterSchool;

    final:

        // 统一更新信息
        CurrentSelectedIndex = currentSelectedIndex ?? -1;
        CurrentState = currentState ?? TimeState.None;
        CurrentSubject = currentSubject ?? Subject.Empty;
        NextClassSubject = nextClassSubject ?? Subject.Empty;
        CurrentTimeLayoutItem = currentTimeLayoutItem ?? TimeLayoutItem.Empty;
        NextClassTimeLayoutItem = nextClassTimeLayoutItem ?? TimeLayoutItem.Empty;
        NextBreakingTimeLayoutItem = nextBreakingTimeLayoutItem ?? TimeLayoutItem.Empty;
        OnClassLeftTime = AtLeastZero(onClassLeftTime) ?? TimeSpan.Zero;
        OnBreakingTimeLeftTime = AtLeastZero(onBreakingTimeLeftTime) ?? TimeSpan.Zero;
        IsLessonConfirmed = isLessonConfirmed ?? false;
        IsClassPlanLoaded = isClassPlanLoaded ?? false;

        // 发出状态变更事件
        if (CurrentState != CurrentOverlayEventStatus)
        {
            CurrentTimeStateChanged?.Invoke(this, EventArgs.Empty);
            switch (CurrentState)
            {
                // 上课事件
                case TimeState.OnClass:
                    OnClass?.Invoke(this, EventArgs.Empty);
                    break;
                // 下课事件
                case TimeState.Breaking:
                    OnBreakingTime?.Invoke(this, EventArgs.Empty);
                    break;
                // 放学事件
                case TimeState.AfterSchool:
                    OnAfterSchool?.Invoke(this, EventArgs.Empty);
                    break;
                case TimeState.None:
                case TimeState.PrepareOnClass:
                default:
                    break;
            }
            CurrentOverlayEventStatus = CurrentState;
        }
    }

    static TimeSpan? AtLeastZero(TimeSpan? a) => a < TimeSpan.Zero ? TimeSpan.Zero : a;

    private int GetClassIndex(int index)
    {
        if (index < 0 || index >= CurrentClassPlan?.TimeLayout.Layouts.Count )
        {
            return -1;
        }
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
        RefreshMultiWeekRotation();
        var currentTime = ExactTimeService.GetCurrentLocalDateTime();
        if (Profile.TempClassPlanSetupTime.Date < currentTime.Date)  // 清除过期临时课表
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

        CurrentClassPlan = GetClassPlanByDate(currentTime);
        var orderedClassPlanId = Profile.OrderedSchedules[currentTime.Date]?.ClassPlanId;
        if (orderedClassPlanId != null 
            && Profile.ClassPlans.TryGetValue(orderedClassPlanId, out var classPlan)
            && classPlan.IsOverlay)
        {
            Profile.OverlayClassPlanId = orderedClassPlanId;
        }
        else
        {
            Profile.OverlayClassPlanId = null;
        }
    }

    private bool CheckClassPlan(ClassPlan plan, DateTime time)
    {
        if (plan.IsOverlay || !plan.IsEnabled)
            return false;

        if (plan.TimeRule.WeekDay != (int)time.DayOfWeek)
        {
            return false;
        }

        if (plan.AssociatedGroup != ClassPlanGroup.GlobalGroupGuid.ToString() &&
            plan.AssociatedGroup != Profile.SelectedClassPlanGroupId &&
            plan.AssociatedGroup != Profile.TempClassPlanGroupId)
        {
            return false;
        }

        if (plan.TimeRule.WeekCountDiv == 0)
            return true;

        // RefreshMultiWeekRotation();
        var rotation = GetMultiWeekRotationByTime(time);
        return plan.TimeRule.WeekCountDiv == rotation[plan.TimeRule.WeekCountDivTotal];
    }

    public void RefreshMultiWeekRotation()
    {
        MultiWeekRotation = GetMultiWeekRotationByTime(ExactTimeService.GetCurrentLocalDateTime());
    }

    private ObservableCollection<int> GetMultiWeekRotationByTime(DateTime time)
    {
        var rotation = new ObservableCollection<int>(DefaultMultiWeekRotation);
        var deltaDays = (time.Date - Settings.SingleWeekStartTime.Date).TotalDays;
        var deltaWeeks = (int)Math.Floor(deltaDays / 7);
        for (var i = 2; i <= 4; i++)
        {
            var w = (deltaWeeks - Settings.MultiWeekRotationOffset[i] + i) % i;
            if (w < 0)
            {
                w += i;
            }
            rotation[i] = w + 1;
        }

        return rotation;
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