using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Extensions;
using ClassIsland.Models;
using ClassIsland.Models.Rules;
using ClassIsland.Shared.ComponentModels;
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
        get;
        set => SetProperty(ref field, value);
    }

    public int CurrentSelectedIndex
    {
        get;
        set => SetProperty(ref field, value);
    } = -1;

    public Subject NextClassSubject
    {
        get;
        set => SetProperty(ref field, value);
    } = Subject.Fallback;

    //public TimeLayoutItem NextTimeLayoutItem
    //{
    //    get => _nextTimeLayoutItem;
    //    set => SetProperty(ref _nextTimeLayoutItem, value);
    //}

    public TimeLayoutItem NextBreakingTimeLayoutItem
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeLayoutItem.Empty;

    public TimeLayoutItem NextClassTimeLayoutItem
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeLayoutItem.Empty;

    public TimeSpan OnClassLeftTime
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeSpan.Zero;

    public TimeState CurrentState
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeState.None;

    public TimeState CurrentOverlayStatus
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeState.None;

    public TimeLayoutItem CurrentTimeLayoutItem
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeLayoutItem.Empty;

    public Subject? CurrentSubject
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsClassPlanEnabled
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool IsClassPlanLoaded
    {
        get;
        set => SetProperty(ref field, value);
    } = false;

    public bool IsLessonConfirmed
    {
        get;
        set => SetProperty(ref field, value);
    } = false;

    public TimeSpan OnBreakingTimeLeftTime
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeSpan.Zero;

    public Dictionary<DateOnly, WeakReference<ClassPlan>> ConvertedScheduleItemsCache { get; } = new();

    public HashSet<DateOnly> DirtyScheduleItemDates { get; } = [];

    private readonly Dictionary<Guid, ScheduleItemSubscription> _scheduleItemSubscriptions = new();
    private readonly IDisposable _scheduleProfileSubscription;
    private IDisposable? _scheduleItemsCollectionSubscription;

    private sealed class ScheduleItemSubscription(ScheduleItem item, TimeRule ruleSnapshot) : IDisposable
    {
        public ScheduleItem Item { get; } = item;
        public TimeRule RuleSnapshot { get; set; } = ruleSnapshot;
        public TimeRule? ObservedRule { get; set; }
        public ObservableCollection<DateOnly>? ObservedEnableDates { get; set; }
        public IDisposable? ItemPropertySubscription { get; set; }
        public IDisposable? RulePropertySubscription { get; set; }
        public IDisposable? EnableDatesCollectionSubscription { get; set; }

        public void Dispose()
        {
            ItemPropertySubscription?.Dispose();
            RulePropertySubscription?.Dispose();
            EnableDatesCollectionSubscription?.Dispose();
        }
    }

    private static TimeRule CreateTimeRuleSnapshot(TimeRule rule)
    {
        return new TimeRule
        {
            Type = rule.Type,
            RestrictsEnableRange = rule.RestrictsEnableRange,
            RangeEnd = rule.RangeEnd,
            RangeStart = rule.RangeStart,
            WeekDay = rule.WeekDay,
            WeekCountDiv = rule.WeekCountDiv,
            WeekCountDivTotal = rule.WeekCountDivTotal,
            EnableDates = new ObservableCollection<DateOnly>(rule.EnableDates),
            LoopCycleDays = rule.LoopCycleDays,
            LoopOffsetDays = rule.LoopOffsetDays
        };
    }

    private void UpdateScheduleModeSubscriptions()
    {
        ReleaseScheduleItemSubscriptions();
        if (Profile.ScheduleType != ScheduleType.Schedule)
        {
            return;
        }

        PruneConvertedScheduleItemsCache();
        DirtyScheduleItemDates.UnionWith(ConvertedScheduleItemsCache.Keys);

        var scheduleItems = Profile.ScheduleItems;
        foreach (var (id, scheduleItem) in scheduleItems)
        {
            SubscribeScheduleItem(id, scheduleItem);
        }

        _scheduleItemsCollectionSubscription = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => scheduleItems.CollectionChanged += handler,
                handler => scheduleItems.CollectionChanged -= handler)
            .Subscribe(x => OnScheduleItemsCollectionChanged(x.EventArgs));
    }

    private void ReleaseScheduleItemSubscriptions()
    {
        _scheduleItemsCollectionSubscription?.Dispose();
        _scheduleItemsCollectionSubscription = null;
        foreach (var subscription in _scheduleItemSubscriptions.Values)
        {
            subscription.Dispose();
        }
        _scheduleItemSubscriptions.Clear();
    }

    private void SubscribeScheduleItem(Guid id, ScheduleItem scheduleItem)
    {
        if (_scheduleItemSubscriptions.Remove(id, out var oldSubscription))
        {
            oldSubscription.Dispose();
        }

        var subscription = new ScheduleItemSubscription(
            scheduleItem,
            CreateTimeRuleSnapshot(scheduleItem.EnableRule));
        _scheduleItemSubscriptions.Add(id, subscription);
        subscription.ItemPropertySubscription = Observable
            .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => scheduleItem.PropertyChanged += handler,
                handler => scheduleItem.PropertyChanged -= handler)
            .Subscribe(_ => OnScheduleItemChanged(subscription));
        ResetScheduleItemRuleSubscriptions(subscription);
    }

    private void UnsubscribeScheduleItem(Guid id)
    {
        if (_scheduleItemSubscriptions.Remove(id, out var subscription))
        {
            subscription.Dispose();
        }
    }

    private void ResetScheduleItemRuleSubscriptions(ScheduleItemSubscription subscription)
    {
        subscription.RulePropertySubscription?.Dispose();
        subscription.EnableDatesCollectionSubscription?.Dispose();

        var rule = subscription.Item.EnableRule;
        subscription.ObservedRule = rule;
        subscription.RulePropertySubscription = Observable
            .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => rule.PropertyChanged += handler,
                handler => rule.PropertyChanged -= handler)
            .Subscribe(_ => OnScheduleItemChanged(subscription));
        ResetScheduleItemEnableDatesSubscription(subscription);
    }

    private void ResetScheduleItemEnableDatesSubscription(ScheduleItemSubscription subscription)
    {
        subscription.EnableDatesCollectionSubscription?.Dispose();

        var enableDates = subscription.Item.EnableRule.EnableDates;
        subscription.ObservedEnableDates = enableDates;
        subscription.EnableDatesCollectionSubscription = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => enableDates.CollectionChanged += handler,
                handler => enableDates.CollectionChanged -= handler)
            .Subscribe(_ => OnScheduleItemChanged(subscription));
    }

    private void OnScheduleItemChanged(ScheduleItemSubscription subscription)
    {
        InvalidateScheduleItemDates([subscription.RuleSnapshot, subscription.Item.EnableRule]);
        subscription.RuleSnapshot = CreateTimeRuleSnapshot(subscription.Item.EnableRule);

        if (!ReferenceEquals(subscription.ObservedRule, subscription.Item.EnableRule))
        {
            ResetScheduleItemRuleSubscriptions(subscription);
        }
        else if (!ReferenceEquals(subscription.ObservedEnableDates, subscription.Item.EnableRule.EnableDates))
        {
            ResetScheduleItemEnableDatesSubscription(subscription);
        }
    }

    private void OnScheduleItemsCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        if (args.Action == NotifyCollectionChangedAction.Move)
        {
            return;
        }

        var oldItems = args.OldItems?.OfType<KeyValuePair<Guid, ScheduleItem>>().ToList() ?? [];
        var newItems = args.NewItems?.OfType<KeyValuePair<Guid, ScheduleItem>>().ToList() ?? [];
        var affectedRules = new List<TimeRule>();

        if (args.Action == NotifyCollectionChangedAction.Reset)
        {
            affectedRules.AddRange(_scheduleItemSubscriptions.Values.Select(x => x.RuleSnapshot));
            affectedRules.AddRange(Profile.ScheduleItems.Values.Select(x => x.EnableRule));
        }
        else
        {
            foreach (var (id, scheduleItem) in oldItems)
            {
                affectedRules.Add(_scheduleItemSubscriptions.TryGetValue(id, out var subscription)
                    && ReferenceEquals(subscription.Item, scheduleItem)
                        ? subscription.RuleSnapshot
                        : CreateTimeRuleSnapshot(scheduleItem.EnableRule));
            }
            affectedRules.AddRange(newItems.Select(x => x.Value.EnableRule));
        }

        InvalidateScheduleItemDates(affectedRules);

        if (args.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var subscription in _scheduleItemSubscriptions.Values)
            {
                subscription.Dispose();
            }
            _scheduleItemSubscriptions.Clear();
            foreach (var (id, scheduleItem) in Profile.ScheduleItems)
            {
                SubscribeScheduleItem(id, scheduleItem);
            }
            return;
        }

        foreach (var (id, _) in oldItems)
        {
            UnsubscribeScheduleItem(id);
        }
        foreach (var (id, scheduleItem) in newItems)
        {
            SubscribeScheduleItem(id, scheduleItem);
        }
    }

    private void InvalidateScheduleItemDates(IEnumerable<TimeRule> rules)
    {
        if (Profile.ScheduleType != ScheduleType.Schedule)
        {
            return;
        }

        PruneConvertedScheduleItemsCache();
        var affectedRules = rules.ToList();
        foreach (var date in ConvertedScheduleItemsCache.Keys)
        {
            if (affectedRules.Any(rule => IsTimeRuleSatisfied(rule, date.ToDateTime(TimeOnly.MinValue))))
            {
                DirtyScheduleItemDates.Add(date);
            }
        }
    }

    private ClassPlan? GetConvertedClassPlanFromScheduleItemsByDate(DateOnly date)
    {
        ClassPlan? classPlan = null;
        if (ConvertedScheduleItemsCache.TryGetValue(date, out var cpRef)
            && cpRef.TryGetTarget(out classPlan)
            && !DirtyScheduleItemDates.Contains(date))
        {
            return classPlan;
        }

        var scheduleItems = Profile.ScheduleItems
            .Where(x => IsTimeRuleSatisfied(x.Value.EnableRule, date.ToDateTime(TimeOnly.MinValue)))
            .OrderBy(x => x.Value.StartTime)
            .ToList();

        if (scheduleItems.Count <= 0)
        {
            DirtyScheduleItemDates.Remove(date);
            ConvertedScheduleItemsCache.Remove(date);
            return null;
        }

        var phonyTimeLayoutId = classPlan?.TimeLayoutId ?? Guid.NewGuid();
        var timeLayout = classPlan?.TimeLayout ?? new TimeLayout()
        {
            Name = date.ToString()
        };
        classPlan ??= new ClassPlan()
        {
            Name = date.ToString(),
            TimeLayouts = new ObservableOrderedDictionary<Guid, TimeLayout>()
            {
                {phonyTimeLayoutId, timeLayout}
            },
            TimeLayoutId = phonyTimeLayoutId
        };

        timeLayout.Layouts.Clear();
        for (var i = 0; i < scheduleItems.Count; i++)
        {
            var (_, item) = scheduleItems[i];
            if (i > 0)
            {
                var (_, prev) = scheduleItems[i - 1];
                timeLayout.Layouts.Add(new TimeLayoutItem()
                {
                    StartTime = prev.EndTime,
                    EndTime = item.StartTime,
                    TimeType = 1
                });
            }
            timeLayout.Layouts.Add(new TimeLayoutItem()
            {
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                TimeType = 0
            });
        }

        classPlan.RefreshClassesList();
        for (int i = 0; i < scheduleItems.Count; i++)
        {
            var (_, item) = scheduleItems[i];
            classPlan.Classes[i].SubjectId = item.SubjectId;
        }
        classPlan.MakeValidTimeLayoutItemsDirty();

        DirtyScheduleItemDates.Remove(date);
        ConvertedScheduleItemsCache[date] = new WeakReference<ClassPlan>(classPlan);

        return classPlan;
    }

    private void PruneConvertedScheduleItemsCache()
    {
        var dates = ConvertedScheduleItemsCache
            .Where(x => !x.Value.TryGetTarget(out _))
            .Select(x => x.Key)
            .ToList();

        foreach (var rm in dates)
        {
            ConvertedScheduleItemsCache.Remove(rm);
        }
    }

    public ClassPlan? GetClassPlanByDate(DateTime date) => GetClassPlanByDate(date, out _);

    public ClassPlan? GetClassPlanByDate(DateTime date, out Guid? guid)
    {
        guid = null;
        if (Profile.ScheduleType == ScheduleType.Schedule)
        {
            return GetConvertedClassPlanFromScheduleItemsByDate(DateOnly.FromDateTime(date));
        }

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
            Profile.ClassPlans.TryGetValue(Profile.TempClassPlanId ?? Guid.Empty, out var tempClassPlan) &&
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
                var matchGlobal = group == ClassPlanGroup.GlobalGroupGuid;
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
                if (group == ClassPlanGroup.GlobalGroupGuid) return 1;
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

        _scheduleProfileSubscription = Observable
            .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => Profile.PropertyChanged += handler,
                handler => Profile.PropertyChanged -= handler)
            .Where(x => x.EventArgs.PropertyName is nameof(Profile.ScheduleType) or nameof(Profile.ScheduleItems))
            .Subscribe(_ => UpdateScheduleModeSubscriptions());
        UpdateScheduleModeSubscriptions();


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
                i.EndTime < now);
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
        if (CurrentClassPlan.TimeLayout != null)
        {
            CurrentClassPlan.TimeLayout.IsActivated = true;
        }

        var now = ExactTimeService.GetCurrentLocalDateTime().TimeOfDay;
        var validTimeLayoutItems = CurrentClassPlan.ValidTimeLayoutItems;

        // 获取当前时间点信息
        currentTimeLayoutItem = validTimeLayoutItems.FirstOrDefault(i =>
            i.TimeType is 0 or 1 &&
            i.StartTime <= now &&
            i.EndTime >= now);
        if (currentTimeLayoutItem != null)
        {
            currentSelectedIndex = layout.IndexOf(currentTimeLayoutItem);
            if (currentTimeLayoutItem.TimeType == 0)
            {
                currentState = TimeState.OnClass;

                var i0 = GetClassIndex((int)currentSelectedIndex);
                if (i0 >= 0 && CurrentClassPlan.Classes.Count > i0 &&
                    Profile.Subjects.TryGetValue(CurrentClassPlan.Classes[i0].SubjectId, out var subject))
                {
                    currentSubject = subject;
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
            i.EndTime >= now);
        if (nextClassTimeLayoutItem != null)
        {
            var i0 = GetClassIndex(layout.IndexOf(nextClassTimeLayoutItem));
            if (i0 >= 0 && CurrentClassPlan.Classes.Count > i0 &&
                Profile.Subjects.TryGetValue(CurrentClassPlan.Classes[i0].SubjectId, out var subject))
                nextClassSubject = subject;
        }
        nextBreakingTimeLayoutItem = validTimeLayoutItems.FirstOrDefault(i =>
            i.TimeType == 1 &&
            i.EndTime >= now);

        // 获取剩余时间信息
        if (currentState == TimeState.OnClass)
            onBreakingTimeLeftTime = nextBreakingTimeLayoutItem?.StartTime - now;
        else
            onClassLeftTime = nextClassTimeLayoutItem?.StartTime - now;

        if (nextClassTimeLayoutItem == null &&
            nextBreakingTimeLayoutItem == null)
            currentState = TimeState.AfterSchool;

    final:

        // 统一更新信息
        CurrentSelectedIndex = currentSelectedIndex ?? -1;
        CurrentState = currentState ?? TimeState.None;
        CurrentSubject = currentSubject ?? Subject.Fallback;
        NextClassSubject = nextClassSubject ?? Subject.Fallback;
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
        if (index < 0 || index >= CurrentClassPlan?.TimeLayout?.Layouts.Count )
        {
            return -1;
        }
        var k = CurrentClassPlan?.TimeLayout?.Layouts[index];
        var l = (from t in CurrentClassPlan?.TimeLayout?.Layouts where t.TimeType == 0 select t).ToList();
        var i = l.IndexOf(k);
        return i;
    }

    public TimeState CurrentOverlayEventStatus
    {
        get;
        set => SetProperty(ref field, value);
    } = TimeState.None;

    private void LoadCurrentClassPlan()
    {
        ProfileService.Profile.RefreshTimeLayouts();
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
        if (Profile.OrderedSchedules.TryGetValue(currentTime.Date, out var orderedSchedule) 
            && Profile.ClassPlans.TryGetValue(orderedSchedule.ClassPlanId, out var classPlan)
            && classPlan.IsOverlay)
        {
            Profile.OverlayClassPlanId = orderedSchedule.ClassPlanId;
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
        
        if (plan.AssociatedGroup != ClassPlanGroup.GlobalGroupGuid &&
            plan.AssociatedGroup != Profile.SelectedClassPlanGroupId &&
            plan.AssociatedGroup != Profile.TempClassPlanGroupId)
        {
            return false;
        }

        return IsTimeRuleSatisfied(plan.TimeRule, time);
    }

    private bool IsTimeRuleSatisfied(TimeRule rule, DateTime time)
    {
        if (rule.RestrictsEnableRange
            && (rule.RangeStart > DateOnly.FromDateTime(time) || rule.RangeEnd < DateOnly.FromDateTime(time)))
        {
            return false;
        }

        switch (rule.Type)
        {
            case TimeRule.TimeRuleType.Weekly:
                if (rule.WeekDay != (int)time.DayOfWeek)
                {
                    return false;
                }
                if (rule.WeekCountDivTotal > SettingsService.Settings.MultiWeekRotationMaxCycle)
                    return false;
                if (rule.WeekCountDiv == 0)
                    return true;
                var rotation = GetCyclePositionsByDate(time);
                return rule.WeekCountDiv == rotation[rule.WeekCountDivTotal];
            case TimeRule.TimeRuleType.Date:
                return rule.EnableDates.Contains(DateOnly.FromDateTime(time));
            case TimeRule.TimeRuleType.Loop:
                var days = (time.Date - Settings.SingleWeekStartTime).Days;
                return days % Math.Max(rule.LoopCycleDays, 1) == rule.LoopOffsetDays;
            default:
                return false;
        }
    }

    /// <summary>
    /// 计算从指定时间起，在多个周期（2周 ~ 最大周期）中的循环位置。
    /// </summary>
    /// <param name="referenceTime">基准时间点。默认为当前时间。</param>
    /// <returns>
    /// 2-first, 1-based
    /// </returns>
    /// <remarks>
    /// 对称逻辑：WeekOffsetSettingsControl.SetMultiWeekRotationOffset()
    /// </remarks>
    public ObservableCollection<int> GetCyclePositionsByDate(DateTime? referenceTime = null)
    {
        referenceTime ??= ExactTimeService.GetCurrentLocalDateTime();
        var cyclePositions = new ObservableCollection<int>([-1, -1]);
        var totalElapsedWeeks = (int)Math.Floor((referenceTime.Value.Date - Settings.SingleWeekStartTime.Date).TotalDays / 7);

        for (int cycleLength = 2; cycleLength <= Settings.MultiWeekRotationMaxCycle; cycleLength++)
        {
            int cycleOffset = Settings.MultiWeekRotationOffset.GetValueOrDefault(cycleLength);
            int positionInCycle = (totalElapsedWeeks + cycleOffset) % cycleLength;
            // 在 C# 中，负数取模为负。
            if (positionInCycle < 0)
                positionInCycle += cycleLength;
            cyclePositions.Add(positionInCycle + 1);
        }
        return cyclePositions;
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
