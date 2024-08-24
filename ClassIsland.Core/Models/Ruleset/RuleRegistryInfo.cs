using System.Diagnostics.CodeAnalysis;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Models.Ruleset;

/// <summary>
/// 代表一个规则的注册信息。
/// </summary>
/// <param name="id">规则集ID，例如“classisland.example”。</param>
/// <param name="name">规则集显示名称。</param>
/// <param name="iconKind">规则集图标。</param>
public class RuleRegistryInfo(string id, string name = "", PackIconKind iconKind = PackIconKind.CogOutline)
{
    /// <summary>
    /// 规则 ID。
    /// </summary>
    public string Id { get; internal set; } = id;

    /// <summary>
    /// 规则显示图标类型。
    /// </summary>
    public PackIconKind IconKind { get; internal set; } = iconKind;


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