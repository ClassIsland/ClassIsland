using System.Collections.ObjectModel;
using AvaloniaEdit.Utils;
namespace ClassIsland.Core.Models.Automation;

/// <summary>
/// 代表一个「添加行动」菜单中的菜单节点，用于构建「添加行动」层叠菜单。
/// </summary>
public abstract class ActionMenuTreeNode(string name, string? iconGlyph)
{
    /// <summary>
    /// 菜单节点名称。
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 菜单节点图标。形如 "\ue9a8" FluentIcon Glyph 格式。支持留空。
    /// </summary>
    public string? IconGlyph { get; } = iconGlyph;
}

/// <inheritdoc cref="ActionMenuTreeItem{T}" />
public class ActionMenuTreeItem : ActionMenuTreeNode
{
    /// <inheritdoc cref="ActionMenuTreeItem{T}" />
    public ActionMenuTreeItem(string actionItemId, string name, string? iconGlyph) : base(name, iconGlyph)
    {
        ActionItemId = actionItemId;
    }

    /// <summary>
    /// 菜单项的行动项 Id。
    /// </summary>
    public string ActionItemId { get; }
}

/// <summary>
/// 代表一个「添加行动」菜单项。
/// </summary>
public class ActionMenuTreeItem<TSettings> : ActionMenuTreeItem where TSettings : new()
{
    /// <inheritdoc cref="ActionMenuTreeItem{T}" />
    public ActionMenuTreeItem(string actionItemId, string name, string? iconGlyph, Action<TSettings> actionItemSettingsSetter) :
        base(actionItemId, name, iconGlyph)
    {
        ActionItemSettingsSetter = actionItemSettingsSetter;
    }

    /// <summary>
    /// 用于对行动项进行设置的 <see cref="Action{TSettings}"/>。
    /// 此方法无返回值，直接对传入的 <typeparamref name="TSettings"/> 进行修改。请勿直接重新赋值。
    /// </summary>
    public Action<TSettings> ActionItemSettingsSetter { get; }
}

/// <summary>
/// 代表一个「添加行动」菜单组。
/// </summary>
public class ActionMenuTreeGroup : ActionMenuTreeNode
{
    /// <inheritdoc cref="ActionMenuTreeGroup" />
    public ActionMenuTreeGroup(string name, string? iconGlyph, params ActionMenuTreeNode[] children) : base(name, iconGlyph)
    {
        Children.AddRange(children);
    }

    /// <summary>
    /// 菜单组的子节点。
    /// </summary>
    public ActionMenuTreeNodeCollection Children { get; } = [];
}

/// <inheritdoc />
public class ActionMenuTreeNodeCollection : KeyedCollection<string, ActionMenuTreeNode>
{
    /// <inheritdoc />
    protected override string GetKeyForItem(ActionMenuTreeNode item) => item.Name;

    /// <summary>
    /// 获取指定名称的 <see cref="ActionMenuTreeGroup"/>。
    /// </summary>
    /// <param name="groupName">菜单组中文名称。</param>
    /// <exception cref="ArgumentException">如果无此菜单组，抛出。</exception>
    public ActionMenuTreeNodeCollection this[string groupName]
    {
        get
        {
            if (TryGetValue(groupName, out var node))
            {
                if (node is ActionMenuTreeGroup group)
                    return group.Children;
            }

            throw new ArgumentException($"未找到 ActionMenuTreeGroup “{groupName}”。");
        }
    }
}