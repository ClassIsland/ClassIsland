using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Shared;
using ClassIsland.Shared.ComponentModels;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 规则集服务。
/// </summary>
public interface IRulesetService
{
    /// <summary>
    /// 当前置窗口发生变化时触发。
    /// </summary>
    event EventHandler? ForegroundWindowChanged;

    /// <summary>
    /// 当当前状态更新，需要重新判定规则集时触发。
    /// </summary>
    event EventHandler? StatusUpdated;

    /// <summary>
    /// 已经注册的规则列表。
    /// </summary>
    public static Dictionary<string, RuleRegistryInfo> Rules { get; } = new();

    /// <summary>
    /// 判断指定的规则集<see cref="Ruleset"/>是否成立。
    /// </summary>
    /// <param name="ruleset">要检验的规则集。</param>
    /// <returns>如果成立，则返回 true。</returns>
    public bool IsRulesetSatisfied(Ruleset ruleset);

    /// <summary>
    /// 注册规则处理程序。
    /// </summary>
    /// <param name="id">要注册的规则ID</param>
    /// <param name="handler">规则处理程序。</param>
    public void RegisterRuleHandler(string id, RuleRegistryInfo.HandleDelegate handler);

    /// <summary>
    /// 通知当前状态发生变化，需要重新判定规则集。
    /// </summary>
    public void NotifyStatusChanged();
}