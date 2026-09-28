using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.ViewModels;

public partial class MainViewViewModel
{
    private readonly HomeDateSelectionState _dateState = App.GetService<HomeDateSelectionState>();
    private readonly IExactTimeService _exactTimeService = App.GetService<IExactTimeService>();
    private readonly SettingsService _calendarSettings = App.GetService<SettingsService>();
    private readonly CompositeDisposable _calendarSubscriptions = new();
    private SynchronizationContext? _calendarContext;
    private bool _calendarActive;
    private bool _calendarRefreshQueued;
    private bool _refreshingCalendar;
    private bool _synchronizingCalendarDate;
    private int _calendarGeneration;
    private DateTime _selectedDate;
    private DateTime _today;

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (!SetProperty(ref _selectedDate, value.Date)) return;
            _dateState.SelectedDate = value.Date;
            _synchronizingCalendarDate = true;
            CalendarDisplayDate = value.Date;
            _synchronizingCalendarDate = false;
            UpdateDatePresentation();
            RefreshCalendarData();
        }
    }

    // Calendar.SelectedDate is nullable; clearing a calendar selection must not clear the displayed day.
    public DateTime? CalendarSelectedDate
    {
        get => SelectedDate;
        set { if (value.HasValue) SelectedDate = value.Value; }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthCalendarVisible))]
    private bool _isCalendarExpanded;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthCalendarVisible))]
    [NotifyCanExecuteChangedFor(nameof(ToggleCalendarCommand))]
    private bool _isCalendarPinned;
    [ObservableProperty] private DateTime _calendarDisplayDate;
    [ObservableProperty] private IReadOnlyList<HomeCalendarDay> _weekDays = [];
    [ObservableProperty] private IReadOnlyDictionary<DateTime, HomeCalendarDay> _calendarDays = new Dictionary<DateTime, HomeCalendarDay>();
    [ObservableProperty] private ClassPlan? _displayedClassPlan;
    [ObservableProperty] private int _displayedSelectedIndex = -1;
    [ObservableProperty] private bool _hasDisplayedLessons;

    public bool IsViewingToday => SelectedDate == _today;
    public bool IsMonthCalendarVisible => IsCalendarPinned || IsCalendarExpanded;
    public string CalendarCaption => (IsMonthCalendarVisible ? CalendarDisplayDate : SelectedDate).ToString("yyyy年M月", CultureInfo.InvariantCulture);
    public string PreviousPeriodDescription => IsMonthCalendarVisible ? "上个月" : "上一周";
    public string NextPeriodDescription => IsMonthCalendarVisible ? "下个月" : "下一周";
    public string CalendarExpansionDescription => IsCalendarExpanded ? "收起月历" : "展开月历";
    public static IReadOnlyList<string> WeekdayNames { get; } = ["一", "二", "三", "四", "五", "六", "日"];

    public void ActivateCalendar()
    {
        if (_calendarActive) return;
        _calendarActive = true;
        _calendarContext = SynchronizationContext.Current;
        _selectedDate = _dateState.SelectedDate ?? _exactTimeService.GetCurrentLocalDateTime().Date;
        SynchronizeToday();
        CalendarDisplayDate = SelectedDate;
        OnPropertyChanged(nameof(SelectedDate));
        UpdateDatePresentation();
        LessonsService.PostMainTimerTicked += CalendarTimerTicked;
        RefreshCalendarData();
    }

    public void DeactivateCalendar()
    {
        _calendarActive = false;
        _calendarGeneration++;
        _calendarRefreshQueued = false;
        LessonsService.PostMainTimerTicked -= CalendarTimerTicked;
        _calendarSubscriptions.Clear();
    }

    public void RefreshCalendar()
    {
        if (!_calendarActive) return;
        SynchronizeToday();
        RefreshCalendarData();
    }

    private void SynchronizeToday()
    {
        var today = _exactTimeService.GetCurrentLocalDateTime().Date;
        var followedToday = _dateState.LastToday == SelectedDate;
        _today = today;
        _dateState.LastToday = today;
        if (followedToday && SelectedDate != today) SelectedDate = today;
        _dateState.SelectedDate = SelectedDate;
        OnPropertyChanged(nameof(IsViewingToday));
    }

    private void CalendarTimerTicked(object? sender, EventArgs e)
    {
        if (_today != _exactTimeService.GetCurrentLocalDateTime().Date)
        {
            SynchronizeToday();
            RefreshCalendarData();
        }
        if (!IsViewingToday) return;
        DisplayedClassPlan = LessonsService.CurrentClassPlan;
        DisplayedSelectedIndex = LessonsService.CurrentSelectedIndex;
    }

    partial void OnIsCalendarExpandedChanged(bool value) => UpdateCalendarMode();

    partial void OnIsCalendarPinnedChanged(bool value) => UpdateCalendarMode();

    private void UpdateCalendarMode()
    {
        CalendarDisplayDate = SelectedDate;
        UpdateDatePresentation();
        RefreshCalendarData();
    }

    partial void OnCalendarDisplayDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(CalendarCaption));
        if (IsMonthCalendarVisible && !_synchronizingCalendarDate) RefreshCalendarData();
    }

    private void UpdateDatePresentation()
    {
        OnPropertyChanged(nameof(CalendarSelectedDate));
        OnPropertyChanged(nameof(IsViewingToday));
        OnPropertyChanged(nameof(CalendarCaption));
        OnPropertyChanged(nameof(PreviousPeriodDescription));
        OnPropertyChanged(nameof(NextPeriodDescription));
        OnPropertyChanged(nameof(CalendarExpansionDescription));
        PreviousPeriodCommand.NotifyCanExecuteChanged();
        NextPeriodCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectDate(DateTime date) => SelectedDate = date;

    [RelayCommand]
    private void GoToToday()
    {
        SynchronizeToday();
        SelectedDate = _today;
        CalendarDisplayDate = SelectedDate;
        RefreshCalendarData();
    }

    private bool CanToggleCalendar() => !IsCalendarPinned;

    [RelayCommand(CanExecute = nameof(CanToggleCalendar))]
    private void ToggleCalendar() => IsCalendarExpanded = !IsCalendarExpanded;

    private bool CanMovePeriod(int direction) => IsMonthCalendarVisible
        ? direction < 0 ? SelectedDate.Year > 1 || SelectedDate.Month > 1 : SelectedDate.Year < 9999 || SelectedDate.Month < 12
        : direction < 0 ? SelectedDate >= DateTime.MinValue.AddDays(7) : SelectedDate <= DateTime.MaxValue.Date.AddDays(-7);

    private bool CanMovePrevious() => CanMovePeriod(-1);
    private bool CanMoveNext() => CanMovePeriod(1);

    [RelayCommand(CanExecute = nameof(CanMovePrevious))]
    private void PreviousPeriod() => MovePeriod(-1);

    [RelayCommand(CanExecute = nameof(CanMoveNext))]
    private void NextPeriod() => MovePeriod(1);

    private void MovePeriod(int direction)
    {
        if (!CanMovePeriod(direction)) return;
        SelectedDate = IsMonthCalendarVisible ? SelectedDate.AddMonths(direction) : SelectedDate.AddDays(direction * 7);
    }

    private static DateTime WeekStart(DateTime date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    private static int CountLessons(ClassPlan? plan) => plan?.TimeLayout?.Layouts.Count(x => x.TimeType == 0) ?? 0;

    private void RefreshCalendarData()
    {
        if (!_calendarActive || _refreshingCalendar) return;
        _refreshingCalendar = true;
        try
        {
            // Queries can update converted schedule caches; don't observe their intermediate changes.
            _calendarSubscriptions.Clear();
            DisplayedClassPlan = IsViewingToday ? LessonsService.CurrentClassPlan : LessonsService.GetClassPlanByDate(SelectedDate, out _);
            DisplayedSelectedIndex = IsViewingToday ? LessonsService.CurrentSelectedIndex : -1;
            HasDisplayedLessons = CountLessons(DisplayedClassPlan) > 0;
            var days = new Dictionary<DateTime, HomeCalendarDay>();
            var weekStart = WeekStart(SelectedDate);
            AddDays(weekStart, 7);
            if (IsMonthCalendarVisible)
            {
                var first = new DateTime(CalendarDisplayDate.Year, CalendarDisplayDate.Month, 1);
                var start = WeekStart(first);
                // Avalonia includes a full preceding week when a month starts on the first weekday.
                if (start == first && start >= DateTime.MinValue.AddDays(7)) start = start.AddDays(-7);
                AddDays(start, 42);
            }
            CalendarDays = days;
            WeekDays = days.Values.Where(x => x.Date >= weekStart && (x.Date - weekStart).Days < 7).OrderBy(x => x.Date).ToArray();
            ObserveCalendarInputs();

            void AddDays(DateTime start, int count)
            {
                for (var i = 0; i < count && i <= (DateTime.MaxValue.Date - start).Days; i++)
                {
                    var date = start.AddDays(i);
                    if (days.ContainsKey(date)) continue;
                    var plan = date == _today ? LessonsService.CurrentClassPlan : LessonsService.GetClassPlanByDate(date, out _);
                    days[date] = new HomeCalendarDay(date, CountLessons(plan), date == _today, date == SelectedDate);
                }
            }
        }
        finally { _refreshingCalendar = false; }
    }

    private void QueueCalendarRefresh()
    {
        if (!_calendarActive || _calendarRefreshQueued || _refreshingCalendar) return;
        _calendarRefreshQueued = true;
        var generation = _calendarGeneration;
        if (_calendarContext == null) { Refresh(null); return; }
        _calendarContext.Post(Refresh, null);
        void Refresh(object? state)
        {
            if (!_calendarActive || generation != _calendarGeneration) return;
            _calendarRefreshQueued = false;
            RefreshCalendarData();
        }
    }

    private void ObserveCalendarInputs()
    {
        var profile = ProfileService.Profile;
        Watch(ProfileService);
        Watch(profile);
        Watch(_calendarSettings);
        Watch(_calendarSettings.Settings);
        Watch(_calendarSettings.Settings.MultiWeekRotationOffset);
        Watch(profile.ScheduleItems);
        Watch(profile.ClassPlans);
        Watch(profile.TimeLayouts);
        Watch(profile.OrderedSchedules);
        Watch(profile.Subjects);
        foreach (var item in profile.ScheduleItems.Values) { Watch(item); WatchRule(item.EnableRule); }
        foreach (var plan in profile.ClassPlans.Values)
        {
            Watch(plan);
            WatchRule(plan.TimeRule);
            Watch(plan.Classes);
            foreach (var lesson in plan.Classes) Watch(lesson);
        }
        foreach (var layout in profile.TimeLayouts.Values)
        {
            Watch(layout);
            Watch(layout.Layouts);
            foreach (var item in layout.Layouts) Watch(item);
        }
        foreach (var schedule in profile.OrderedSchedules.Values) Watch(schedule);
        foreach (var subject in profile.Subjects.Values) Watch(subject);
        PropertyChangedEventHandler changed = (_, e) =>
        {
            if (e.PropertyName == nameof(ILessonsService.CurrentClassPlan)) QueueCalendarRefresh();
        };
        LessonsService.PropertyChanged += changed;
        _calendarSubscriptions.Add(Disposable.Create(() => LessonsService.PropertyChanged -= changed));
    }

    private void WatchRule(TimeRule rule) { Watch(rule); Watch(rule.EnableDates); }

    private void Watch(object source)
    {
        if (source is INotifyCollectionChanged collection)
        {
            NotifyCollectionChangedEventHandler changed = (_, _) => QueueCalendarRefresh();
            collection.CollectionChanged += changed;
            _calendarSubscriptions.Add(Disposable.Create(() => collection.CollectionChanged -= changed));
        }
        else if (source is INotifyPropertyChanged observable)
        {
            PropertyChangedEventHandler changed = (_, _) => QueueCalendarRefresh();
            observable.PropertyChanged += changed;
            _calendarSubscriptions.Add(Disposable.Create(() => observable.PropertyChanged -= changed));
        }
    }
}
