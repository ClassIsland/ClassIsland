using System;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.ruleSet.rulesetChanged", "规则集更新时", PackIconKind.TagMultipleOutline)]
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