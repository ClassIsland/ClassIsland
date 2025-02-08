using System;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.lessons.onAfterSchool", "放学时", PackIconKind.ExitRun)]
public class OnAfterSchoolTrigger(ILessonsService lessonsService) : TriggerBase
{
    private ILessonsService LessonsService { get; } = lessonsService;

    public override void Loaded()
    {
        LessonsService.OnAfterSchool += OnLessonsServiceOnAfterSchool;
    }
    public override void UnLoaded()
    {
        LessonsService.OnAfterSchool -= OnLessonsServiceOnAfterSchool;
    }

    private void OnLessonsServiceOnAfterSchool(object? sender, EventArgs e)
    {
        Trigger();
    }
}