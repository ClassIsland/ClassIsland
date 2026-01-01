using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Platform;
using ClassIsland.Models.External.ClassWidgets;
using ClassIsland.Services;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Helpers.ProfileTransferHelpers;

public static class ClassWidgetsProfileTransferHelper
{
    public static Profile ConvertClassWidgets1ProfileToClassIslandProfile(string path, Profile? profile = null)
    {
        var profileCw = ConfigureFileHelper.LoadConfigUnWrapped<CwProfile>(path);
        var templateJson = new StreamReader(AssetLoader.Open(new Uri("avares://ClassIsland/Assets/default-subjects.json"))).ReadToEnd();
        profile ??= JsonSerializer.Deserialize<Profile>(templateJson)!;
        
        // Subjects
        var subjectsCache = profile.Subjects.ToDictionary(x => x.Value.Name, x => x.Key);
        foreach (var subjectName in profileCw.Schedule.Values.ToList().SelectMany(x => x))
        {
            subjectsCache.TryAdd(subjectName, Guid.NewGuid());
        }
        foreach (var subjectName in profileCw.ScheduleEven.Values.ToList().SelectMany(x => x))
        {
            subjectsCache.TryAdd(subjectName, Guid.NewGuid());
        }
        foreach (var (name, guid) in subjectsCache)
        {
            profile.Subjects.TryAdd(guid, new Subject()
            {
                Name = name
            });
        }
        
        // TimeLayouts
        // 注意！cw 课表结构中，计数从周一（0）开始。
        var parts = new Dictionary<string, CwProfileTimeSpan?>();
        
        
        foreach (var (index, value) in profileCw.Part.OrderBy(x => int.Parse(x.Key)))
        {
            if (value.Count < 3 || value[0] is not JsonElement { ValueKind: JsonValueKind.Number } startHour 
                                || value[1] is not JsonElement { ValueKind: JsonValueKind.Number } endHour 
                                || value[2] is not JsonElement { ValueKind: JsonValueKind.String } type 
                                || profileCw.PartName.GetValueOrDefault(index) is not {} name)
            {
                parts.Add(index, null);
                continue;
            }
            parts.Add(index, new CwProfileTimeSpan()
            {
                Name = name,
                StartTime = new TimeSpan(startHour.GetInt32(), endHour.GetInt32(), 0),
                Type = type.GetString() switch
                {
                    "break" => CwProfileTimeSpan.TimeSpanType.Break,
                    _ => CwProfileTimeSpan.TimeSpanType.Part
                }
            });
        }

        var (timeLayouts, timeLayoutsMap) = ImportCwTimeLine(profileCw.Timeline, "单周");
        var (timeLayoutsEven, timeLayoutsMapEven) = ImportCwTimeLine(profileCw.TimelineEven, "双周");

        foreach (var (id, timeLayout) in timeLayouts)
        {
            profile.TimeLayouts[timeLayoutsMap[id]] = timeLayout;
        }
        foreach (var (id, timeLayout) in timeLayoutsEven)
        {
            profile.TimeLayouts[timeLayoutsMapEven[id]] = timeLayout;
        }
        
        // ClassPlans
        // 注意！cw 课表结构中，计数从周一（0）开始。
        List<string> days = ["6", "0", "1", "2", "3", "4", "5"];
        List<string> daysName = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];
        var undefinedClassName = "未添加";
        for (int i = 0; i < 7; i++)
        {
            var day = days[i];
            var cpOdd = new ClassPlan()
            {
                TimeLayoutId = timeLayouts.ContainsKey(day) ? timeLayoutsMap[day] : timeLayoutsMap["default"],
                TimeLayouts = profile.TimeLayouts
            };
            var cpEven = new ClassPlan()
            {
                TimeLayoutId = timeLayoutsEven.ContainsKey(day) ? timeLayoutsMapEven[day] : timeLayoutsMapEven["default"],
                TimeLayouts = profile.TimeLayouts
            };
            cpEven.TimeRule.WeekDay = cpOdd.TimeRule.WeekDay = i;
            cpOdd.Name = cpEven.Name = daysName[i];
            var shouldSplitClassPlans = profileCw.Schedule[day].Count != profileCw.ScheduleEven[day].Count;
            // 清除自动生成的课程安排
            cpEven.Classes.Clear();
            cpOdd.Classes.Clear();
            for (int j = 0; j < Math.Min(profileCw.Schedule[day].Count, profileCw.ScheduleEven[day].Count); j++)
            {
                var subjectOdd = profileCw.Schedule[day][j];
                var subjectEven = profileCw.ScheduleEven[day][j];
                cpOdd.Classes.Add(new ClassInfo()
                {
                    SubjectId = subjectsCache[subjectOdd]
                });
                cpEven.Classes.Add(new ClassInfo()
                {
                    SubjectId = subjectsCache[subjectEven]
                });
                if (subjectEven != subjectOdd)
                {
                    shouldSplitClassPlans = true;
                }
            }

            var oddValid = !subjectsCache.ContainsKey(undefinedClassName) || cpOdd.Classes.Any(x => x.SubjectId != subjectsCache[undefinedClassName]);
            var evenValid = !subjectsCache.ContainsKey(undefinedClassName) || cpEven.Classes.Any(x => x.SubjectId != subjectsCache[undefinedClassName]);
            var evenAdded = false;
            if (oddValid)
            {
                profile.ClassPlans.TryAdd(Guid.NewGuid(), cpOdd);
            }
            else if (evenValid)
            {
                profile.ClassPlans.TryAdd(Guid.NewGuid(), cpEven);
                evenAdded = true;
            }
            
            if (!shouldSplitClassPlans || !evenValid || !oddValid)
            {
                continue;
            }
            cpOdd.Name += "（单周）";
            cpEven.Name += "（双周）";
            cpOdd.TimeRule.WeekCountDivTotal = cpOdd.TimeRule.WeekCountDivTotal = 2;
            cpEven.TimeRule.WeekCountDiv = 2;
            cpOdd.TimeRule.WeekCountDiv = 1;
            if (!evenAdded)
            {
                profile.ClassPlans.TryAdd(Guid.NewGuid(), cpEven);
            }
        }

        foreach (var (_, cp) in profile.ClassPlans)
        {
            cp.RefreshClassesList();
        }
        
        return profile;

        (Dictionary<string, TimeLayout> results, Dictionary<string, Guid> map) ImportCwTimeLine(Dictionary<string, List<List<JsonElement>>> timeLines, string description)
        {
            var myTimeLayouts = new Dictionary<string, TimeLayout>();
            var myTimeLayoutsMap = new Dictionary<string, Guid>([
                new KeyValuePair<string, Guid>("default", Guid.NewGuid())
            ]);
            for (int i = 0; i < 7; i++)
            {
                myTimeLayoutsMap.Add(i.ToString(), Guid.NewGuid());
            }
            foreach (var (id, timeLine) in timeLines.Where(x => x.Value.Count > 0))
            {
                var timeLineNormalized = timeLine
                    .Where(x => x is
                                [
                                    { ValueKind: JsonValueKind.Number }, { ValueKind: JsonValueKind.String },
                                    { ValueKind: JsonValueKind.Number }, { ValueKind: JsonValueKind.Number }
                                ]
                                && parts.GetValueOrDefault(x[1].GetString() ?? "") != null)
                    .OrderBy(x => parts[x[1].GetString() ?? ""]?.StartTime)
                    .Select(x => new
                    {
                        TimeType = x[0].GetInt32(),
                        PartId = x[1].GetString(),
                        IndexOfPart = x[2].GetInt32(),
                        DurationMinutes = x[3].GetDouble()
                    })
                    .ToList();
                if (timeLineNormalized.Count <= 0)
                {
                    continue;
                }

                var timeLayout = new TimeLayout()
                {
                    Name = id switch
                    {
                        "default" => "默认",
                        "0" => "周一",
                        "1" => "周二",
                        "2" => "周三",
                        "3" => "周四",
                        "4" => "周五",
                        "5" => "周六",
                        "6" => "周日",
                        _ => id
                    } + $"（{description}）"
                };
                var groupIndex = 0;
                var prevGroup = timeLineNormalized[0].PartId;
                var prevEnd = parts[prevGroup]!.StartTime;
                foreach (var i in timeLineNormalized)
                {
                    var group = i.PartId;
                    if (group != prevGroup)
                    {
                        prevGroup = group;
                        groupIndex++;
                        timeLayout.Layouts.Add(new TimeLayoutItem()
                        {
                            TimeType = 2,
                            StartTime = parts[group]!.StartTime - TimeSpan.FromSeconds(1)
                        });
                        prevEnd = parts[group]!.StartTime;
                    }

                    var start = prevEnd;
                    var end = prevEnd = start + TimeSpan.FromMinutes(i.DurationMinutes);
                    timeLayout.Layouts.Add(new TimeLayoutItem()
                    {
                        TimeType = i.TimeType,
                        StartTime = start,
                        EndTime = end
                    });
                }
                myTimeLayouts[id] = timeLayout;
            }

            return (myTimeLayouts, myTimeLayoutsMap);
        }
    }
}