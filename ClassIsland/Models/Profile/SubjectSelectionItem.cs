using System;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Models.Profile;

/// <summary>
/// 科目选择器中的一项。分组标题使用 <see cref="IsGroupHeader"/> 标记，不能被选中。
/// </summary>
public sealed class SubjectSelectionItem
{
    public SubjectSelectionItem(Guid key, Subject? value, string? groupName = null)
    {
        Key = key;
        Value = value;
        GroupName = groupName;
    }

    public Guid Key { get; }

    public Subject? Value { get; }

    public string? GroupName { get; }

    public bool IsGroupHeader => Value == null;
}
