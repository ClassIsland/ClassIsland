using System;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.ruleSet.rulesetChanged", "取决于规则集", "\uf17e")]
public class RulesetChangedTrigger(IRulesetService rulesetService) : TriggerBase
{
    private IRulesetService RulesetService { get; } = rulesetService;

    public override void Loaded()
    {
        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
    }

    public override void UnLoaded()
    {
        RulesetService.StatusUpdated -= RulesetServiceOnStatusUpdated;
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        Trigger();
    }
}