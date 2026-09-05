using System.Text.Json;
using ClassIsland.Services.Management;
using ClassIsland.Shared.Models.Profile;

static void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
using var data = JsonDocument.Parse("""
[
 {"weekday":1,"subject_name":"语文","teacher_name":"张老师","starts_at":"08:00:00","ends_at":"08:40:00"},
 {"weekday":1,"subject_name":"数学","teacher_name":null,"starts_at":"08:50:00","ends_at":"09:30:00"},
 {"weekday":5,"subject_name":"英语","starts_at":"14:00:00","ends_at":"14:35:00"}
]
""");
var profile = JsonSerializer.Deserialize<Profile>(JsonSerializer.Serialize(new Profile()))!;
profile.TempClassPlanId = Guid.Parse("bb001122-3344-5566-7788-99aabbccdde1");
BashuScheduleMapper.Apply(profile, data.RootElement);
Check(profile.TempClassPlanId == null, "legacy temporary plan is removed");
Check(profile.ClassPlans.Count == 7, "seven distinct weekday plans");
var monday = profile.ClassPlans.Values.Single(x => x.TimeRule.WeekDay == 1);
var friday = profile.ClassPlans.Values.Single(x => x.TimeRule.WeekDay == 5);
Check(monday.Classes.Count == 2 && friday.Classes.Count == 1, "Monday and Friday are not copies");
Check(profile.TimeLayouts[friday.TimeLayoutId].Layouts.Last().EndTime == TimeSpan.FromMinutes(875), "custom Friday end time preserved");
Check(profile.TimeLayouts[monday.TimeLayoutId].Layouts.Count == 3, "break does not offset subject indices");
Check(profile.Subjects[monday.Classes[0].SubjectId].TeacherName == "张老师", "teacher identity preserved");
var subjects = profile.Subjects.Count;
BashuScheduleMapper.Apply(profile, data.RootElement);
Check(profile.Subjects.Count == subjects && profile.ClassPlans.Count == 7, "repeat sync is idempotent");
using var empty = JsonDocument.Parse("[]");
BashuScheduleMapper.Apply(profile, empty.RootElement);
Check(profile.ClassPlans.Values.All(x => x.Classes.Count == 0), "empty schedule clears previous lessons");
var shortDuration = BashuNotificationTiming.Duration("请准备课本", "张老师", 1);
var longDuration = BashuNotificationTiming.Duration(new string('课', 300), "张老师", 1);
Check(longDuration > shortDuration && longDuration.TotalSeconds > 100, "long notification gets enough reading time");
Check(BashuNotificationTiming.Duration("请准备课本", "张老师", 3) == shortDuration * 3, "repeat count scales scrolling duration");
