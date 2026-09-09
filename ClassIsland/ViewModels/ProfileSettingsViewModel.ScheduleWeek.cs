using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Threading;
using ClassIsland.Controls.ScheduleWeekEdit;
using ClassIsland.Models;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class ProfileSettingsViewModel
{
    [ObservableProperty] private DateOnly _scheduleWeekStart;
    [ObservableProperty] private DateTime? _scheduleWeekNavigationDate;
    [ObservableProperty] private Guid? _selectedScheduleItemId;
    [ObservableProperty] private DateOnly? _scheduleWeekSelectedDate;
    [ObservableProperty] private TimeSpan? _scheduleWeekSelectedTime;
    [ObservableProperty] private IReadOnlyList<ScheduleWeekOccurrence> _scheduleWeekItems = [];
    private readonly CompositeDisposable _scheduleWeekSubscriptions = new();
    private bool _scheduleWeekRefreshQueued;

    public string ScheduleWeekCaption
    {
        get
        {
            // Match ScheduleDataGrid's week numbering, which uses the week's Saturday as its reference.
            var referenceDate = ScheduleWeekStart.AddDays(5).ToDateTime(TimeOnly.MinValue);
            var week = (int)Math.Ceiling((referenceDate - SettingsService.Settings.SingleWeekStartTime).TotalDays / 7);
            return $"第 {week} 周";
        }
    }
    public DateTime ScheduleWeekNavigationMaxDate => DateTime.MaxValue.Date.AddDays(-(((int)DateTime.MaxValue.DayOfWeek + 1) % 7));

    private void InitializeScheduleWeek()
    {
        var today = DateOnly.FromDateTime(ExactTimeService.GetCurrentLocalDateTime());
        ScheduleWeekNavigationDate = today.ToDateTime(TimeOnly.MinValue);
        RefreshScheduleWeek();
    }

    partial void OnScheduleWeekStartChanged(DateOnly value)
    {
        OnPropertyChanged(nameof(ScheduleWeekCaption));
        var navigationDate = ScheduleWeekNavigationDate;
        if (navigationDate == null || DateOnly.FromDateTime(navigationDate.Value).DayNumber < value.DayNumber
            || DateOnly.FromDateTime(navigationDate.Value).DayNumber > value.DayNumber + 6)
        {
            var offset = navigationDate == null ? 0 : ((int)navigationDate.Value.DayOfWeek + 6) % 7;
            ScheduleWeekNavigationDate = value.AddDays(offset).ToDateTime(TimeOnly.MinValue);
        }
        ScheduleWeekSelectedDate = null;
        ScheduleWeekSelectedTime = null;
        QueueScheduleWeekRefresh();
    }

    partial void OnScheduleWeekNavigationDateChanged(DateTime? value)
    {
        if (value == null) return;
        var date = DateOnly.FromDateTime(value.Value);
        var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        if (monday.DayNumber <= DateOnly.MaxValue.DayNumber - 6)
            ScheduleWeekStart = monday;
    }

    partial void OnSelectedScheduleItemIdChanged(Guid? value)
    {
        SelectedScheduleItemKvp = value is { } id && ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item)
            ? new KeyValuePair<Guid, ScheduleItem>(id, item) : null;
    }

    public void MoveScheduleWeek(int weeks)
    {
        var day = ScheduleWeekStart.DayNumber + weeks * 7;
        if (day < 0 || day > DateOnly.MaxValue.DayNumber - 6) return;
        ScheduleWeekStart = DateOnly.FromDayNumber(day);
    }

    public void GoToCurrentScheduleWeek()
    {
        var today = DateOnly.FromDateTime(ExactTimeService.GetCurrentLocalDateTime());
        ScheduleWeekNavigationDate = today.ToDateTime(TimeOnly.MinValue);
    }

    private void QueueScheduleWeekRefresh()
    {
        if (_resourcesReleased || _scheduleWeekRefreshQueued) return;
        _scheduleWeekRefreshQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _scheduleWeekRefreshQueued = false;
            if (!_resourcesReleased) RefreshScheduleWeek();
        });
    }

    private void RefreshScheduleWeek()
    {
        OnPropertyChanged(nameof(ScheduleWeekCaption));
        _scheduleWeekSubscriptions.Clear();
        var profile = ProfileService.Profile;
        WatchProperties(profile);
        WatchProperties(SettingsService);
        if (ProfileService is INotifyPropertyChanged profileService) WatchProperties(profileService);
        WatchProperties(SettingsService.Settings);
        WatchCollection(SettingsService.Settings.MultiWeekRotationOffset);
        WatchCollection(profile.ScheduleItems);
        WatchCollection(profile.Subjects);
        foreach (var item in profile.ScheduleItems.Values)
        {
            WatchProperties(item);
            WatchProperties(item.EnableRule);
            WatchCollection(item.EnableRule.EnableDates);
        }
        foreach (var subject in profile.Subjects.Values) WatchProperties(subject);

        var occurrences = new List<ScheduleWeekOccurrence>();
        for (var day = 0; day < 7; day++)
        {
            var date = ScheduleWeekStart.AddDays(day);
            foreach (var (id, item) in LessonsService.GetScheduleItemsByDate(date))
            {
                var name = profile.Subjects.TryGetValue(item.SubjectId, out var subject) && !string.IsNullOrWhiteSpace(subject.Name)
                    ? subject.Name : "未指定科目";
                occurrences.Add(new ScheduleWeekOccurrence(id, date, name, item.StartTime, item.EndTime));
            }
        }
        if (!ScheduleWeekItems.SequenceEqual(occurrences))
            ScheduleWeekItems = occurrences;
        if (SelectedScheduleItemId is { } selectedId && !profile.ScheduleItems.ContainsKey(selectedId))
            SelectedScheduleItemId = null;
    }

    private void WatchProperties(INotifyPropertyChanged source)
    {
        PropertyChangedEventHandler handler = (_, _) => QueueScheduleWeekRefresh();
        source.PropertyChanged += handler;
        _scheduleWeekSubscriptions.Add(Disposable.Create(() => source.PropertyChanged -= handler));
    }

    private void WatchCollection(INotifyCollectionChanged source)
    {
        NotifyCollectionChangedEventHandler handler = (_, _) => QueueScheduleWeekRefresh();
        source.CollectionChanged += handler;
        _scheduleWeekSubscriptions.Add(Disposable.Create(() => source.CollectionChanged -= handler));
    }

    public KeyValuePair<Guid, ScheduleItem> CreateScheduleItem(DateOnly? date = null, TimeSpan? startTime = null)
    {
        var start = startTime ?? TimeSpan.Zero;
        var end = TimeSpan.FromMinutes(Math.Min(1440,
            start.TotalMinutes + Math.Max(0, SettingsService.Settings.DefaultOnClassTimePointMinutes)));
        var item = new ScheduleItem { EndTime = end, StartTime = start };
        if (date is { } selectedDate)
            item.EnableRule.WeekDay = (int)selectedDate.DayOfWeek;
        var pair = new KeyValuePair<Guid, ScheduleItem>(Guid.NewGuid(), item);
        ScheduleItems.List.Add(pair);
        SelectedScheduleItemKvp = pair;
        return pair;
    }

    public bool ApplyScheduleWeekEdit(ScheduleWeekEditEventArgs request)
    {
        if (request.ScheduleItemId is not { } id || !ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item))
            return false;
        var delta = request.Date.DayNumber - request.OriginalDate.DayNumber;
        var rule = ConfigureFileHelper.CopyObject(item.EnableRule);
        try
        {
            if (delta != 0)
            {
                switch (rule.Type)
                {
                    case TimeRule.TimeRuleType.Weekly:
                        rule.WeekDay = (int)request.Date.DayOfWeek;
                        break;
                    case TimeRule.TimeRuleType.Date:
                        rule.EnableDates = new ObservableCollection<DateOnly>(rule.EnableDates.Select(date => date.AddDays(delta)));
                        break;
                    case TimeRule.TimeRuleType.Loop:
                        var cycle = Math.Max(1, rule.LoopCycleDays);
                        rule.LoopOffsetDays = (int)((((long)rule.LoopOffsetDays + delta) % cycle + cycle) % cycle);
                        break;
                }
                if (rule.RestrictsEnableRange)
                {
                    var start = rule.RangeStart.AddDays(delta);
                    var end = rule.RangeEnd.AddDays(delta);
                    // Range setters constrain each other, so extend the range before moving its opposite edge.
                    if (delta > 0) { rule.RangeEnd = end; rule.RangeStart = start; }
                    else { rule.RangeStart = start; rule.RangeEnd = end; }
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        if (request.StartTime < TimeSpan.Zero || request.EndTime > TimeSpan.FromDays(1) || request.EndTime < request.StartTime)
            return false;
        if (request.StartTime >= item.EndTime)
        {
            item.EndTime = request.EndTime;
            item.StartTime = request.StartTime;
        }
        else
        {
            item.StartTime = request.StartTime;
            item.EndTime = request.EndTime;
        }
        if (delta != 0) item.EnableRule = rule;
        // The edit control must receive the committed times before releasing its drag preview.
        RefreshScheduleWeek();
        return true;
    }
}
