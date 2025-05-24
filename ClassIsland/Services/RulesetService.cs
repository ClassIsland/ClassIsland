using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.Ruleset;
using System.Text.Json;

namespace ClassIsland.Services;

public class RulesetService : IRulesetService
{
    public ILogger<RulesetService> Logger { get; }


    public RulesetService(ILogger<RulesetService> logger)
    {
        Logger = logger;
        NotifyStatusChanged();
    }

    

    public event EventHandler? ForegroundWindowChanged;

    public event EventHandler? StatusUpdated;

    private int BoolToRuleObjectState(bool? v) => v switch
    {
        true => 2,
        false => 1,
        null => 0
    };

    private bool? IsRuleSatisfied(Rule i)
    {
        if (i.Id == string.Empty)
            return null;

        if (!IRulesetService.Rules.TryGetValue(i.Id, out var rule))
        {
            Logger.LogWarning("找不到规则 {} 的注册信息，已默认其结果为 false.", i.Id);
            return false;
        }

        object? settings = null;
        var settingsType = rule.SettingsType;
        if (settingsType != null)
        {
            settings = i.Settings ?? Activator.CreateInstance(settingsType);
            if (settings is JsonElement json)
            {
                settings = json.Deserialize(settingsType);
            }
        }
        if (rule.Handle != null)
        {
            return rule.Handle(settings);
        }
        else
        {
            Logger.LogWarning("规则 {} 的处理程序没有注册，已默认其结果为 false.", rule.Id);
            return false;
        }
    }

    private bool? IsRulesetGroupSatisfied(RuleGroup ruleset)
    {
        var rulesetSatisfied = ruleset.Mode == RulesetLogicalMode.And;
        if (ruleset.Rules.Where(r => r.Id != "").ToList().Count <= 0)
        {
            return null;
        }

        foreach (var i in ruleset.Rules)
        {
            bool? res = IsRuleSatisfied(i);
            if (res == null)
            {
                i.State = BoolToRuleObjectState(res);
                continue;
            }

            bool result = (bool)res;
            result ^= i.IsReversed;
            i.State = BoolToRuleObjectState(result);
            if (!result && ruleset.Mode == RulesetLogicalMode.And)
            {
                rulesetSatisfied = false;
                break;
            }
            if (result && ruleset.Mode == RulesetLogicalMode.Or)
            {
                rulesetSatisfied = true;
                break;
            }
        }
        rulesetSatisfied ^= ruleset.IsReversed;
        return rulesetSatisfied;
    }

    public bool IsRulesetSatisfied(Ruleset ruleset)
    {
        var isSatisfied = ruleset.Mode == RulesetLogicalMode.And;
        if (ruleset.Groups.Count <= 0)
        {
            ruleset.State = BoolToRuleObjectState(false);
            return false;
        }
        foreach (var i in ruleset.Groups)
        {
            i.State = 0;
            foreach (var j in i.Rules)
            {
                j.State = 0;
            }
        }
        foreach (var group in ruleset.Groups.Where(x => x.IsEnabled))
        {
            bool? res = IsRulesetGroupSatisfied(group);
            group.State = BoolToRuleObjectState(res);
            if (res == null)
                continue;

            bool result = (bool)res;
            if (!result && ruleset.Mode == RulesetLogicalMode.And)
            {
                isSatisfied = false;
                break;
            }
            if (result && ruleset.Mode == RulesetLogicalMode.Or)
            {
                isSatisfied = true;
                break;
            }
        }
        isSatisfied ^= ruleset.IsReversed;
        ruleset.State = BoolToRuleObjectState(isSatisfied);
        return isSatisfied;
    }

    public void RegisterRuleHandler(string id, RuleRegistryInfo.HandleDelegate handler)
    {
        if (!IRulesetService.Rules.TryGetValue(id, out var ruleRegistryInfo))
        {
            throw new KeyNotFoundException($"找不到规则 {id}。");
        }

        ruleRegistryInfo.Handle += handler;
    }

    public void NotifyStatusChanged()
    {
        StatusUpdated?.Invoke(this, EventArgs.Empty);
    }
}
