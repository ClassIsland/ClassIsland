using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public class NamedRulesetService : INamedRulesetService
{
    private readonly ILogger<NamedRulesetService> _logger;

    public ObservableCollection<NamedRuleset> NamedRulesets { get; } = new();

    public event EventHandler? NamedRulesetsUpdated;

    public NamedRulesetService(ILogger<NamedRulesetService> logger)
    {
        _logger = logger;
        LoadConfig();
    }

    public NamedRuleset AddNamedRuleset(string name = "", string description = "")
    {
        var ruleset = new NamedRuleset
        {
            Name = string.IsNullOrWhiteSpace(name) ? "未命名规则集" : name,
            Description = description
        };
        NamedRulesets.Add(ruleset);
        SaveConfig();
        NamedRulesetsUpdated?.Invoke(this, EventArgs.Empty);
        return ruleset;
    }

    public void RemoveNamedRuleset(Guid id)
    {
        var ruleset = NamedRulesets.FirstOrDefault(x => x.Id == id);
        if (ruleset != null)
        {
            RemoveNamedRuleset(ruleset);
        }
    }

    public void RemoveNamedRuleset(NamedRuleset ruleset)
    {
        NamedRulesets.Remove(ruleset);
        SaveConfig();
        NamedRulesetsUpdated?.Invoke(this, EventArgs.Empty);
    }

    public NamedRuleset? GetNamedRuleset(Guid id)
    {
        return NamedRulesets.FirstOrDefault(x => x.Id == id);
    }

    public void SaveConfig()
    {
        try
        {
            _logger.LogInformation("保存共享规则集配置");
            ConfigureFileHelper.SaveConfig(
                Path.Combine(CommonDirectories.AppRootFolderPath, "NamedRulesets.json"),
                NamedRulesets.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存共享规则集配置失败");
        }
    }

    public void LoadConfig()
    {
        try
        {
            _logger.LogInformation("加载共享规则集配置");
            var path = Path.Combine(CommonDirectories.AppRootFolderPath, "NamedRulesets.json");
            if (File.Exists(path))
            {
                var loaded = ConfigureFileHelper.LoadConfig<List<NamedRuleset>>(path);
                NamedRulesets.Clear();
                foreach (var ruleset in loaded)
                {
                    NamedRulesets.Add(ruleset);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载共享规则集配置失败");
        }
    }
}
