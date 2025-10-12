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
        var timeLayouts = new Dictionary<string, TimeLayout>();
        var timeLayoutsMap = new Dictionary<string, Guid>([
            new KeyValuePair<string, Guid>("default", Guid.NewGuid())
        ]);
        for (int i = 0; i < 7; i++)
        {
            timeLayoutsMap.Add(i.ToString(), Guid.NewGuid());
        }
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
                Type = type.GetRawText() switch
                {
                    "break" => CwProfileTimeSpan.TimeSpanType.Break,
                    _ => CwProfileTimeSpan.TimeSpanType.Part
                }
            });
        }

        foreach (var (id, timeLine) in profileCw.Timeline.Where(x => x.Value.Count > 0))
        {
            var timeLineNormalized = timeLine
                .Where(x => x.Key.Length == 3
                            && parts.GetValueOrDefault(x.Key[1].ToString()) != null
                            && int.TryParse(x.Key[1].ToString(), out _)
                            && double.TryParse(x.Value, out _))
                .OrderBy(x => int.Parse(x.Key[1].ToString()))
                .ToList();
            if (timeLineNormalized.Count <= 0)
            {
                continue;
            }

            var timeLayout = new TimeLayout();
            var groupIndex = 0;
            var prevGroup = timeLineNormalized[0].Key[1].ToString();
            var prevEnd = parts[prevGroup]!.StartTime;
            foreach (var (k, v) in timeLineNormalized)
            {
                var group = k[1].ToString();
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
                var end = prevEnd = start + TimeSpan.FromMinutes(double.Parse(v));
                timeLayout.Layouts.Add(new TimeLayoutItem()
                {
                    TimeType = k[0] == 'f' ? 1 : 0,
                    StartTime = start,
                    EndTime = end
                });
            }
            timeLayouts[id] = timeLayout;
        }

        foreach (var (id, timeLayout) in timeLayouts)
        {
            profile.TimeLayouts[timeLayoutsMap[id]] = timeLayout;
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
                TimeLayoutId = timeLayouts.ContainsKey(day) ? timeLayoutsMap[day] : timeLayoutsMap["default"],
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

            var oddValid = cpOdd.Classes.Any(x => x.SubjectId != subjectsCache[undefinedClassName]);
            var evenValid = cpEven.Classes.Any(x => x.SubjectId != subjectsCache[undefinedClassName]);
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
            cpEven.TimeRule.WeekCountDiv = 0;
            cpOdd.TimeRule.WeekCountDiv = 1;
            if (!evenAdded)
            {
                profile.ClassPlans.TryAdd(Guid.NewGuid(), cpEven);
            }
        }

        return profile;
    }
}