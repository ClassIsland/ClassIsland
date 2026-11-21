using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Attributes;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class MainView
{
    internal async Task ShowPageAsync(string pageId)
    {
        if (MainNavigation.MenuItems.OfType<FANavigationViewItem>()
                .FirstOrDefault(x => x.Tag is MainViewPageInfo info && info.Id == pageId) is { } item)
        {
            _pendingNavigationItem = item;
            await NavigatePendingPagesAsync();
        }
    }

    internal void ToggleNavigationPane() => MainNavigation.IsPaneVisible = !MainNavigation.IsPaneVisible;

    internal async Task ShowSystemScheduleAsync(DateTime date, string? lessonId)
    {
        await ShowPageAsync("classisland.today");
        ViewModel.SelectedDate = date.Date;
        ViewModel.RefreshCalendar();
        if (Guid.TryParse(lessonId, out var scheduleId))
        {
            if (ViewModel.GetScheduleItemIds(DateOnly.FromDateTime(date)).Contains(scheduleId))
            {
                await ShowCourseInfoAsync(scheduleId, DateOnly.FromDateTime(date));
            }
        }
        else if (int.TryParse(lessonId, NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
                 ViewModel.DisplayedClassPlan is { } plan && index >= 0 && index < plan.Classes.Count)
        {
            await ShowClassicCourseInfoAsync(plan.Classes[index], DateOnly.FromDateTime(date));
        }
    }
}
