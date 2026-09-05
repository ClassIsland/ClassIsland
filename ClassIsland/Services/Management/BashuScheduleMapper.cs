using System;
using System.Linq;
using System.Text.Json;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Services.Management;

public static class BashuScheduleMapper
{
    public static Guid PlanId(int weekday) => Guid.Parse($"bb001122-3344-5566-7788-99aabbccdd{weekday:00}");

    public static void Apply(Profile profile, JsonElement scheduleEl)
    {
        // Remove only the legacy platform-generated temporary plan.
        var legacy = Guid.Parse("bb001122-3344-5566-7788-99aabbccdde1");
        if (profile.TempClassPlanId == legacy) profile.TempClassPlanId = null;
        profile.ClassPlans.Remove(legacy);
        profile.TimeLayouts.Remove(Guid.Parse("bb001122-3344-5566-7788-99aabbccdde0"));
        for (var day = 1; day <= 7; day++)
        {
            var planId = PlanId(day);
            var layoutId = Guid.Parse($"bb001122-3344-5566-7788-99aabbccee{day:00}");
            var layout = new TimeLayout { Name = $"平台作息 · 周{"一二三四五六日"[day - 1]}", IsActivated = true };
            var plan = new ClassPlan
            {
                Name = $"平台课表 · 周{"一二三四五六日"[day - 1]}",
                TimeLayoutId = layoutId, IsEnabled = true, IsActivated = true,
                AssociatedGroup = ClassPlanGroup.GlobalGroupGuid
            };
            plan.TimeRule.WeekDay = day % 7;
            plan.TimeRule.WeekCountDiv = 0;
            TimeSpan? previousEnd = null;
            foreach (var lesson in scheduleEl.EnumerateArray()
                .Where(x => x.TryGetProperty("weekday", out var weekday) && int.TryParse(weekday.ToString(), out var value) && value == day)
                .OrderBy(x => x.GetProperty("starts_at").GetString()))
            {
                var start = TimeSpan.Parse(lesson.GetProperty("starts_at").GetString()!);
                var end = TimeSpan.Parse(lesson.GetProperty("ends_at").GetString()!);
                if (end <= start || (previousEnd.HasValue && start < previousEnd)) throw new InvalidOperationException("平台课表时间重叠或无效");
                if (previousEnd.HasValue && start > previousEnd.Value)
                    layout.Layouts.Add(new TimeLayoutItem { StartTime = previousEnd.Value, EndTime = start, TimeType = 1, BreakName = "课间休息" });
                layout.Layouts.Add(new TimeLayoutItem { StartTime = start, EndTime = end, TimeType = 0 });
                var name = lesson.TryGetProperty("subject_name", out var subjectEl) ? subjectEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(name) || name == "自习") name = "自主学习";
                var teacher = lesson.TryGetProperty("teacher_name", out var teacherEl) ? teacherEl.GetString() ?? "" : "";
                var subject = profile.Subjects.FirstOrDefault(x => x.Value.Name == name && x.Value.TeacherName == teacher);
                var subjectId = subject.Key;
                if (subjectId == Guid.Empty)
                {
                    subjectId = Guid.NewGuid();
                    profile.Subjects[subjectId] = new Subject { Name = name, Initial = name[..1], TeacherName = teacher };
                }
                plan.Classes.Add(new ClassInfo { Index = plan.Classes.Count, SubjectId = subjectId, CurrentTimeLayout = layout });
                previousEnd = end;
            }
            // Attach only after construction: loaded profiles observe layout changes and
            // would otherwise insert placeholder classes while the plan is still being built.
            profile.TimeLayouts[layoutId] = layout;
            profile.ClassPlans[planId] = plan;
        }

    }
}
