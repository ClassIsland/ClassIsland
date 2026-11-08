using System;
using System.Collections.ObjectModel;
using System.Linq;
using ClassIsland.Controls.ScheduleWeekEdit;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Helpers;

internal static class ScheduleItemEditHelper
{
    public static bool ApplyScheduleWeekEdit(Profile profile, ScheduleWeekEditEventArgs request)
    {
        if (request.ScheduleItemId is not { } id ||
            !profile.ScheduleItems.TryGetValue(id, out var item))
        {
            return false;
        }

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
                        rule.EnableDates = new ObservableCollection<DateOnly>(
                            rule.EnableDates.Select(date => date.AddDays(delta)));
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
                    if (delta > 0)
                    {
                        rule.RangeEnd = end;
                        rule.RangeStart = start;
                    }
                    else
                    {
                        rule.RangeStart = start;
                        rule.RangeEnd = end;
                    }
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if (request.StartTime < TimeSpan.Zero ||
            request.EndTime > TimeSpan.FromDays(1) ||
            request.EndTime <= request.StartTime)
        {
            return false;
        }

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

        if (delta != 0)
        {
            item.EnableRule = rule;
        }

        return true;
    }
}
