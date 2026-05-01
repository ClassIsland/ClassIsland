using System;
using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Ruleset;

namespace ClassIsland.Core.Abstractions.Services;

public interface INamedRulesetService
{
    ObservableCollection<NamedRuleset> NamedRulesets { get; }

    event EventHandler? NamedRulesetsUpdated;

    NamedRuleset AddNamedRuleset(string name = "", string description = "");

    void RemoveNamedRuleset(Guid id);

    void RemoveNamedRuleset(NamedRuleset ruleset);

    NamedRuleset? GetNamedRuleset(Guid id);

    void SaveConfig();

    void LoadConfig();
}
