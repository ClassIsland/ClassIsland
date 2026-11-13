using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Attributes;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class MainView
{
    internal async Task ShowSystemScheduleAsync(DateTime date, string? lessonId)
    {
        if (MainNavigation.MenuItems.OfType<FANavigationViewItem>()
                .FirstOrDefault(x => x.Tag is MainViewPageInfo { Id: "classisland.today" }) is { } todayItem)
        {
            _pendingNavigationItem = todayItem;
            await NavigatePendingPagesAsync();
        }
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
