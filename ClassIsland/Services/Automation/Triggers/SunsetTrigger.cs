using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.sunset", "\u65e5\u843d\u65f6", "\uEC3F")]
public class SunsetTrigger : TriggerBase
{
    private readonly SunriseSunsetTriggerScheduler _scheduler;

    public SunsetTrigger(SunriseSunsetService sunriseSunsetService)
    {
        _scheduler = new SunriseSunsetTriggerScheduler(sunriseSunsetService, SunTransition.Sunset, Trigger);
    }

    public override void Loaded() => _scheduler.Load();

    public override void UnLoaded() => _scheduler.Unload();
}
