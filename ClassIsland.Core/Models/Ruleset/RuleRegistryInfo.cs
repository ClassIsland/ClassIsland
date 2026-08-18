using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Models.Ruleset;

/// <summary>
/// 代表一个规则的注册信息。
/// </summary>
/// <param name="id">规则集ID，例如“classisland.example”。</param>
/// <param name="name">规则集显示名称。</param>
/// <param name="iconExpression">规则集图标表达式。</param>
public class RuleRegistryInfo(string id, string name = "", string iconExpression = "\uef27")
{
    /// <summary>
    /// 规则 ID。
    /// </summary>
    public string Id { get; internal set; } = id;

    /// <summary>
    /// 规则显示图标。
    /// </summary>
    public FAIconSource? IconSource { get; internal set; } =
        IconExpressionHelper.TryParseOrNull(iconExpression);


    /// <summary>
    /// 规则显示名称。
    /// </summary>
    public string Name { get; internal set; } = string.IsNullOrEmpty(name) ? id : name;

    /// <summary>
    /// 设置控件类型。
    /// </summary>
    public Type? SettingsControlType { get; internal set; }

    /// <summary>
    /// 设置类型。
    /// </summary>
    public Type? SettingsType { get; internal set; }

    public delegate bool HandleDelegate(object? settings);

    public HandleDelegate? Handle;
}
