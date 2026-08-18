namespace ClassIsland.Core.Attributes;

using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

/// <summary>
/// 自动化触发器信息。
/// </summary>
/// <param name="id">触发器 ID</param>
/// <param name="name">触发器名称</param>
/// <param name="iconExpression">触发器图标表达式</param>
[AttributeUsage(AttributeTargets.Class)]
public class TriggerInfo(string id, string name, string iconExpression = "\uED55") : Attribute
{
    /// <summary>
    /// 触发器 ID
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// 触发器名称
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 触发器图标
    /// </summary>
    public FAIconSource? IconSource { get; } = IconExpressionHelper.TryParseOrNull(iconExpression);

    /// <summary>
    /// 触发器类型
    /// </summary>
    public Type? TriggerType { get; internal set; }

    /// <summary>
    /// 设置界面类型
    /// </summary>
    public Type? SettingsControlType { get; internal set; }
}
