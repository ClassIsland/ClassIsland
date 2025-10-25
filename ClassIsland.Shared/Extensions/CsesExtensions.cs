using ClassIsland.Shared.Models.Profile;
using CsesSharp.Enums;
using CsesSharp.Models;
using ClassInfo = ClassIsland.Shared.Models.Profile.ClassInfo;
using Profile = ClassIsland.Shared.Models.Profile.Profile;
using Subject = ClassIsland.Shared.Models.Profile.Subject;

namespace ClassIsland.Shared.Extensions;

/// <summary>
/// CSES 格式扩展类
/// </summary>
public static class CsesExtensions
{
    private static DateTime TimeSpanToDateTime(TimeSpan timeSpan) => DateTime.Today.Add(timeSpan);

    /// <summary>
    /// 将 CSES 格式的课表转换为 ClassIsland 格式的课表。
    /// </summary>
    /// <param name="profile">CSES 格式课表</param>
    /// <param name="mergeProfile">需要合并的 ClassIsland 课表</param>
    /// <returns>ClassIsland 格式课表</returns>
    public static Profile ToClassIslandObject(this CsesSharp.Models.Profile profile, Profile? mergeProfile=null)
    {
        var result = mergeProfile ?? new Profile();
        var subjectsCache = new Dictionary<string, Guid>();

        foreach (var i in result.Subjects)
        {
            subjectsCache[i.Value.Name] = i.Key;
        }
        foreach (var i in profile.Subjects)
        {
            if (!subjectsCache.TryGetValue(i.Name, out var id))
            {
                id = Guid.NewGuid();
                subjectsCache[i.Name] = id;
                result.Subjects[id] = new Subject();
            }

            result.Subjects[id].Name = i.Name;
            result.Subjects[id].Initial = string.IsNullOrWhiteSpace(i.SimplifiedName) ? result.Subjects[id].Initial : i.SimplifiedName;
            result.Subjects[id].TeacherName = string.IsNullOrWhiteSpace(i.Teacher) ? result.Subjects[id].TeacherName : i.Teacher;
        }

        foreach (var i in profile.Schedules)
        {
            var timeLayoutKey = result.TimeLayouts.FirstOrDefault(x =>
            {
                var validTimeLayout = x.Value.Layouts.Where(y => y.TimeType == 0).ToList();
                if (validTimeLayout.Count != i.Classes.Count)
                {
                    return false;
                }
                for (var j = 0; j < Math.Min(validTimeLayout.Count, i.Classes.Count); j++)
                {
                    if (validTimeLayout[j].StartTime != i.Classes[j].StartTime ||
                        validTimeLayout[j].EndTime != i.Classes[j].EndTime)
                    {
                        return false;
                    }
                }

                return true;
            }).Key;
            TimeLayout timeLayout;
            bool isNewTimeLayout = timeLayoutKey == Guid.Empty;
            if (isNewTimeLayout)
            {
                timeLayout = new TimeLayout();
                timeLayoutKey = Guid.NewGuid();
            }
            else
            {
                timeLayout = result.TimeLayouts[timeLayoutKey];
            }

            var classPlan = new ClassPlan();
            for (var j = 0; j < i.Classes.Count; j++)
            {
                subjectsCache.TryGetValue(i.Classes[j].Subject, out var subjectId);
                classPlan.Classes.Add(new ClassInfo()
                {
                    SubjectId = subjectId,
                });

                if (isNewTimeLayout)
                {
                    timeLayout.Layouts.Add(new TimeLayoutItem()
                    {
                        TimeType = 0,
                        StartTime = i.Classes[j].StartTime,
                        EndTime = i.Classes[j].EndTime,
                    });
                    if (j < i.Classes.Count - 1)
                    {
                        timeLayout.Layouts.Add(new TimeLayoutItem()
                        {
                            TimeType = 1,
                            StartTime = i.Classes[j].EndTime,
                            EndTime = i.Classes[j + 1].StartTime,
                        });
                    }
                }
            }
            if (isNewTimeLayout)
            {
                result.TimeLayouts[timeLayoutKey] = timeLayout;
            }

            classPlan.TimeLayoutId = timeLayoutKey;
            classPlan.TimeRule.WeekCountDiv = (int)i.Weeks;
            classPlan.TimeRule.WeekDay = (int)i.EnableDay;
            classPlan.Name = i.Name;
            result.ClassPlans[Guid.NewGuid()] = classPlan;
        }

        return result;
    }

    /// <summary>
    /// 将 ClassIsland 格式的课表转换为 CSES 格式的课表。
    /// </summary>
    /// <param name="profile">ClassIsland 格式课表</param>
    /// <param name="mergeProfile">需要合并的 CSES 格式课表</param>
    /// <returns>CSES 格式课表</returns>
    public static CsesSharp.Models.Profile ToCsesObject(this Profile profile, CsesSharp.Models.Profile? mergeProfile = null)
    {
        var result = mergeProfile ?? new CsesSharp.Models.Profile();

        foreach (var i in profile.Subjects.Where(x => !result.Subjects.Exists(y => y.Name == x.Value.Name)).ToList())
        {
            result.Subjects.Add(new CsesSharp.Models.Subject()
            {
                Name = i.Value.Name,
                SimplifiedName = i.Value.Initial,
                Teacher = i.Value.TeacherName,
            });
        }

        profile.RefreshTimeLayouts();
        foreach (var i in profile.ClassPlans.Where(x => x.Value is
                     { TimeLayout: not null, IsEnabled: true, TimeRule.WeekCountDivTotal: <= 2, TimeRule.WeekCountDiv: <= 2 }).ToList())
        {
            i.Value.RefreshClassesList();
            var schedule = new Schedule()
            {
                Name = i.Value.Name,
                Weeks = (WeekType)i.Value.TimeRule.WeekCountDiv,
                EnableDay = (DayOfWeek)i.Value.TimeRule.WeekDay,
            };
            foreach (var j in i.Value.Classes)
            {
                profile.Subjects.TryGetValue(j.SubjectId, out var subject);
                schedule.Classes.Add(new CsesSharp.Models.ClassInfo()
                {
                    StartTime = j.CurrentTimeLayoutItem.StartTime,
                    EndTime = j.CurrentTimeLayoutItem.EndTime,
                    Subject = subject?.Name ?? "",
                });
            }
            result.Schedules.Add(schedule);
        }

        return result;
    }
}