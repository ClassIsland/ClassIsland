using System;
using System.Linq;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Helpers;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.lessons.preTimePoint", "特定时间点前", "\ue4c6")]
public class PreTimePointTrigger(ILessonsService lessonsService, IExactTimeService exactTimeService) : TriggerBase<PreTimePointTriggerSettings>
{
    private ILessonsService LessonsService { get; } = lessonsService;
    private IExactTimeService ExactTimeService { get; } = exactTimeService;

    private DateTime LastCheckTime { get; set; }


    public override void Loaded()
    {
        LastCheckTime = ExactTimeService.GetCurrentLocalDateTime();
        LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
    }


    public override void UnLoaded()
    {
        LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
    }
    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        try
        {
            if (LessonsService.CurrentState == Settings.TargetState)
            {
                TriggerRevert();
                return;
            }
            
            if (Settings.TimeSeconds < 0) return;

            TimeSpan targetStartTime;

            if (Settings.TargetState == TimeState.AfterSchool)
            {
                var afterSchoolTime = LessonsService.CurrentClassPlan?.ValidTimeLayoutItems
                    .LastOrDefault(i => i.TimeType is 0 or 1)?.EndTime;
                if (afterSchoolTime == null) return;
                
                targetStartTime = afterSchoolTime.Value;
                
            }
            else
            {
                var targetTimePoint = Settings.TargetState switch
                {
                    TimeState.OnClass => LessonsService.NextClassTimeLayoutItem,
                    TimeState.Breaking => LessonsService.NextBreakingTimeLayoutItem,
                    _ => TimeLayoutItem.Empty,
                };
                if (targetTimePoint == TimeLayoutItem.Empty) return;

                targetStartTime = targetTimePoint.StartTime;
            }

            var targetTime = targetStartTime - TimeSpanHelper.FromSecondsSafe(Settings.TimeSeconds);
            var targetTime2 = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(targetTime));
            //Console.WriteLine($"{LastCheckTime} {targetTime} {targetTimePoint.StartSecond} {now}");
            if (LastCheckTime < targetTime2 && targetTime2 <= now)
            {
                Trigger();
            }
        }
        finally
        {
            LastCheckTime = now;
        }
    }
}