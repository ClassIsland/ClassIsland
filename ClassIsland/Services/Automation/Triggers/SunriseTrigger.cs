using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.sunrise", "\u65e5\u51fa\u65f6", "\uEC45")]
public class SunriseTrigger : TriggerBase
{
    private readonly SunriseSunsetTriggerScheduler _scheduler;

    public SunriseTrigger(SunriseSunsetService sunriseSunsetService)
    {
        _scheduler = new SunriseSunsetTriggerScheduler(sunriseSunsetService, SunTransition.Sunrise, Trigger);
    }

    public override void Loaded() => _scheduler.Load();

    public override void UnLoaded() => _scheduler.Unload();
}
