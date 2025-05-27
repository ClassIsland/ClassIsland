using System;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.lessons.onClass", "上课时", MaterialIconKind.BookOutline)]
public class OnClassTrigger(ILessonsService lessonsService) : TriggerBase
{
    private ILessonsService LessonsService { get; } = lessonsService;

    public override void Loaded()
    {
        LessonsService.OnClass += LessonsServiceOnOnClass;
    }
    public override void UnLoaded()
    {
        LessonsService.OnClass -= LessonsServiceOnOnClass;
    }

    private void LessonsServiceOnOnClass(object? sender, EventArgs e)
    {
        Trigger();
    }
}