using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models.SystemIntegration;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared.Enums;

namespace ClassIsland.Services.SystemIntegration;

/// <summary>
/// 系统扩展只接收共享排课引擎展开后的课程，不重复实现轮换和调课规则。
/// </summary>
internal sealed class SystemScheduleSnapshotFactory(
    ILessonsService lessonsService,
    IProfileService profileService,
    IExactTimeService exactTimeService)
{
    public SystemScheduleSnapshot Create(bool notificationsEnabled, DateTimeOffset systemNow)
    {
        var logicalNow = exactTimeService.GetCurrentLocalDateTime();
        var profile = profileService.Profile;
        var lessons = new List<SystemScheduleLesson>();
        for (var day = 0; day < 7; day++)
        {
            var date = logicalNow.Date.AddDays(day);
            var plan = lessonsService.GetClassPlanByDate(date);
            if (plan is not { IsEnabled: true, TimeLayout: not null })
            {
                continue;
            }

            var items = plan.TimeLayout.Layouts.Where(x => x.TimeType == 0).ToArray();
            var validItems = plan.ValidTimeLayoutItems.ToHashSet();
            var scheduleIds = profile.ScheduleType == ScheduleType.Schedule
                ? lessonsService.GetScheduleItemsByDate(DateOnly.FromDateTime(date)).Keys.ToArray()
                : [];
            for (var index = 0; index < items.Length && index < plan.Classes.Count; index++)
            {
                var item = items[index];
                var info = plan.Classes[index];
                if (!info.IsEnabled || !validItems.Contains(item) || item.EndTime <= item.StartTime)
                {
                    continue;
                }

                profile.Subjects.TryGetValue(info.SubjectId, out var subject);
                var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var lessonId = index < scheduleIds.Length
                    ? scheduleIds[index].ToString("D")
                    : index.ToString(CultureInfo.InvariantCulture);
                var start = IosNotificationTimeMapper.ToSystemTime(date + item.StartTime, logicalNow, systemNow);
                var end = IosNotificationTimeMapper.ToSystemTime(date + item.EndTime, logicalNow, systemNow);
                lessons.Add(new SystemScheduleLesson(
                    $"{dateText}/{lessonId}",
                    string.IsNullOrWhiteSpace(subject?.Name) ? "未命名课程" : subject.Name,
                    string.IsNullOrWhiteSpace(info.Location) ? subject?.Location ?? "" : info.Location,
                    start.ToUnixTimeSeconds(),
                    end.ToUnixTimeSeconds(),
                    $"classisland://app/system/schedule?date={dateText}&lesson={lessonId}"));
            }
        }

        var classPlans = profile.ClassPlans
            .Where(x => x.Value.IsEnabled && !x.Value.IsOverlay)
            .Take(512)
            .Select(x => new SystemScheduleClassPlan(
                x.Key.ToString("D"),
                string.IsNullOrWhiteSpace(x.Value.Name) ? "未命名课表" : x.Value.Name,
                $"classisland://app/system/view-plan?id={x.Key:D}"))
            .ToArray();
        var validUntil = IosNotificationTimeMapper.ToSystemTime(
            logicalNow.Date.AddDays(7), logicalNow, systemNow);
        return new SystemScheduleSnapshot(
            systemNow.ToUnixTimeSeconds(),
            validUntil.ToUnixTimeSeconds(),
            notificationsEnabled,
            lessons.OrderBy(x => x.StartAt).ThenBy(x => x.Id, StringComparer.Ordinal).Take(2048).ToArray(),
            classPlans);
    }
}
