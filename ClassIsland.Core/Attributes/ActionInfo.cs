using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Attributes;

/// <summary>
/// 行动提供方信息。
/// </summary>
/// <param name="id">行动提供方 ID。形如 "classisland.app.quit"。</param>
/// <param name="name">行动提供方名称。</param>
/// <param name="iconExpression">行动提供方图标表达式。支持留空。</param>
/// <param name="addDefaultToMenu">是否要在「添加行动」菜单添加默认项。默认为 true。</param>
/// <param name="defaultGroupToMenu">在「添加行动」菜单添加默认项的根菜单组。如果找不到该组会添加该组。默认为根菜单。</param>
[AttributeUsage(AttributeTargets.Class)]
public class ActionInfo(string id, string name, string? iconExpression = null, bool addDefaultToMenu = true, string defaultGroupToMenu = "") : Attribute
{
    /// <summary>
    /// 行动提供方 ID。形如 "classisland.app.quit"。
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// 行动提供方名称。
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 行动提供方图标。
    /// </summary>
    public FAIconSource? IconSource { get; } = string.IsNullOrEmpty(iconExpression)
        ? null
        : IconExpressionHelper.TryParseOrNull(iconExpression);

    internal string? IconExpression { get; } = iconExpression;

    /// <summary>
    /// 行动提供方是否支持恢复。
    /// </summary>
    public bool IsRevertable { get; set; }

    /// <summary>
    /// 是否要在「添加行动」菜单添加默认项。
    /// </summary>
    public bool AddDefaultToMenu { get; } = addDefaultToMenu;

    /// <summary>
    /// 在「添加行动」菜单添加默认项的根菜单组。
    /// </summary>
    public string DefaultGroupToMenu { get; } = defaultGroupToMenu;
}
