using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Threading;
using ClassIsland.Controls.ScheduleWeekEdit;
using ClassIsland.Helpers;
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
    private Settings? _observedScheduleWeekSettings;

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
        ObserveScheduleWeekSettings();
        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName != nameof(SettingsService.Settings)) return;
            Dispatcher.UIThread.Post(() =>
            {
                if (_resourcesReleased) return;
                ObserveScheduleWeekSettings();
                OnPropertyChanged(nameof(ScheduleWeekCaption));
            });
        };
        SettingsService.PropertyChanged += handler;
        _externalSubscriptions.Add(Disposable.Create(() =>
        {
            SettingsService.PropertyChanged -= handler;
            if (_observedScheduleWeekSettings != null)
                _observedScheduleWeekSettings.PropertyChanged -= ScheduleWeekSettingsChanged;
        }));
        var today = DateOnly.FromDateTime(ExactTimeService.GetCurrentLocalDateTime());
        ScheduleWeekNavigationDate = today.ToDateTime(TimeOnly.MinValue);
    }

    private void ObserveScheduleWeekSettings()
    {
        if (_observedScheduleWeekSettings != null)
            _observedScheduleWeekSettings.PropertyChanged -= ScheduleWeekSettingsChanged;
        _observedScheduleWeekSettings = SettingsService.Settings;
        _observedScheduleWeekSettings.PropertyChanged += ScheduleWeekSettingsChanged;
    }

    private void ScheduleWeekSettingsChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Settings.SingleWeekStartTime))
            Dispatcher.UIThread.Post(() =>
            {
                if (!_resourcesReleased) OnPropertyChanged(nameof(ScheduleWeekCaption));
            });
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

    public bool RemoveSelectedReplacementScheduleItem()
    {
        if (SelectedScheduleItemKvp is not { } selected
            || selected.Value.ReplacementTarget == null
            || !ScheduleItems.List.Remove(selected))
            return false;

        SelectedScheduleItemKvp = null;
        return true;
    }

    public bool ApplyScheduleWeekEdit(ScheduleWeekEditEventArgs request)
    {
        return ScheduleItemEditHelper.ApplyScheduleWeekEdit(ProfileService.Profile, request);
    }
}
