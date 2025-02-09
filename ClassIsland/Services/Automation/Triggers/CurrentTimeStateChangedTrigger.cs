using System;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.lessons.currentTimeStateChanged", "当前时间状态变化时", PackIconKind.ClockAlertOutline)]
public class CurrentTimeStateChangedTrigger(ILessonsService lessonsService) : TriggerBase
{
    private ILessonsService LessonsService { get; } = lessonsService;

    public override void Loaded()
    {
        LessonsService.CurrentTimeStateChanged += CurrentLessonsServiceOnTimeStateChanged;
    }
    public override void UnLoaded()
    {
        LessonsService.CurrentTimeStateChanged -= CurrentLessonsServiceOnTimeStateChanged;
    }

    private void CurrentLessonsServiceOnTimeStateChanged(object? sender, EventArgs e)
    {
        Trigger();
    }
}