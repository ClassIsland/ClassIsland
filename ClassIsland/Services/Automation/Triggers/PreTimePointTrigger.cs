using System;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Helpers;
using ClassIsland.Models.Automation.Triggers;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.lessons.preTimePoint", "特定时间点前", PackIconKind.ClockEnd)]
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
        var targetTimePoint = Settings.TargetState switch
        {
            TimeState.OnClass => LessonsService.NextClassTimeLayoutItem,
            TimeState.Breaking => LessonsService.NextBreakingTimeLayoutItem,
            _ => TimeLayoutItem.Empty
        };

        var now = ExactTimeService.GetCurrentLocalDateTime();

        try
        {
            if (LessonsService.CurrentState == Settings.TargetState)
            {
                TriggerRevert();
                return;
            }
            if (targetTimePoint == TimeLayoutItem.Empty || Settings.TimeSeconds < 0)
            {
                return;
            }

            var targetTime = targetTimePoint.StartSecond - TimeSpanHelper.FromSecondsSafe(Settings.TimeSeconds);
            var targetTime2 = new DateTime(DateOnly.FromDateTime(now), TimeOnly.FromTimeSpan(targetTime.TimeOfDay));
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