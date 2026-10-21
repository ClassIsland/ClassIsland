using System;
using System.Collections.Generic;
using System.Linq;
using ClassIsland.Controls.ScheduleWeekEdit;
using ClassIsland.Helpers;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class MainViewViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainViewHeader))]
    [NotifyPropertyChangedFor(nameof(CanStartScheduleEditing))]
    [NotifyPropertyChangedFor(nameof(CanAddScheduleItem))]
    [NotifyPropertyChangedFor(nameof(IsScheduleWeekReadonly))]
    private bool _isScheduleEditing;

    public bool IsScheduleMode => ProfileService.Profile.ScheduleType == ScheduleType.Schedule;

    public bool CanEditMobileSchedule => IsScheduleMode && ManagementService.Policy.CanEditProfile;

    public bool CanEditProfileFeatures => ManagementService.Policy.CanEditProfile;

    public string MainViewHeader => IsScheduleEditing ? "正在编辑课表" : "ClassIsland";
    public bool CanStartScheduleEditing => CanEditMobileSchedule && !IsScheduleEditing;
    public bool CanAddScheduleItem => CanEditMobileSchedule && IsScheduleEditing;
    public bool IsScheduleWeekReadonly => !CanEditMobileSchedule || !IsScheduleEditing;

    public void RefreshScheduleModeState()
    {
        if (!IsScheduleMode && IsScheduleEditing)
        {
            IsScheduleEditing = false;
        }
        OnPropertyChanged(nameof(IsScheduleMode));
        OnPropertyChanged(nameof(CanEditMobileSchedule));
        OnPropertyChanged(nameof(CanEditProfileFeatures));
        OnPropertyChanged(nameof(MainViewHeader));
        OnPropertyChanged(nameof(CanStartScheduleEditing));
        OnPropertyChanged(nameof(CanAddScheduleItem));
        OnPropertyChanged(nameof(IsScheduleWeekReadonly));
    }

    public IReadOnlyList<Guid> GetScheduleItemIds(DateOnly date) =>
        LessonsService.GetScheduleItemsByDate(date).Select(x => x.Key).ToList();

    public Guid? FindScheduleItemIdByLesson(DateTime date, TimeLayoutItem timeLayoutItem)
    {
        if (!IsScheduleMode)
        {
            return null;
        }

        var plan = LessonsService.GetClassPlanByDate(date);
        if (plan?.TimeLayout == null)
        {
            return null;
        }

        var valid = plan.TimeLayout.Layouts.Where(x => x.TimeType == 0).ToList();
        var index = valid.IndexOf(timeLayoutItem);
        if (index < 0)
        {
            return null;
        }

        var items = GetScheduleItemIds(DateOnly.FromDateTime(date));
        return index < items.Count ? items[index] : null;
    }

    public ClassInfo? FindClassInfoByLesson(DateTime date, TimeLayoutItem timeLayoutItem)
    {
        var plan = LessonsService.GetClassPlanByDate(date);
        if (plan?.TimeLayout == null)
        {
            return null;
        }

        var valid = plan.TimeLayout.Layouts.Where(x => x.TimeType == 0).ToList();
        var index = valid.IndexOf(timeLayoutItem);
        return index >= 0 && index < plan.Classes.Count ? plan.Classes[index] : null;
    }

    public (Guid Id, ScheduleItem Item) CreateScheduleItem(DateOnly date, TimeSpan? startTime = null)
    {
        var start = startTime ?? TimeSpan.FromHours(8);
        if (start < TimeSpan.Zero || start >= TimeSpan.FromDays(1))
        {
            start = TimeSpan.FromHours(8);
        }

        var end = start.Add(TimeSpan.FromMinutes(
            Math.Max(5, _calendarSettings.Settings.DefaultOnClassTimePointMinutes)));
        if (end > TimeSpan.FromDays(1))
        {
            end = TimeSpan.FromDays(1);
        }

        var item = new ScheduleItem
        {
            StartTime = start,
            EndTime = end
        };
        item.EnableRule.WeekDay = (int)date.DayOfWeek;
        var id = Guid.NewGuid();
        ProfileService.Profile.ScheduleItems.Add(id, item);
        try
        {
            ProfileService.SaveProfile();
        }
        catch
        {
            ProfileService.Profile.ScheduleItems.Remove(id);
            throw;
        }
        return (id, item);
    }

    public bool ApplyScheduleWeekEdit(ScheduleWeekEditEventArgs request)
    {
        if (request.ScheduleItemId is not { } id ||
            !ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item))
        {
            return false;
        }

        var snapshot = ConfigureFileHelper.CopyObject(item);
        if (!ScheduleItemEditHelper.ApplyScheduleWeekEdit(ProfileService.Profile, request))
        {
            return false;
        }

        try
        {
            ProfileService.SaveProfile();
            return true;
        }
        catch
        {
            ProfileService.Profile.ScheduleItems[id] = snapshot;
            throw;
        }
    }

    public bool ToggleScheduleItemEnabled(Guid id)
    {
        if (!ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item))
        {
            return false;
        }

        var old = item.IsEnabled;
        item.IsEnabled = !item.IsEnabled;
        try
        {
            ProfileService.SaveProfile();
            return true;
        }
        catch
        {
            item.IsEnabled = old;
            throw;
        }
    }
}
