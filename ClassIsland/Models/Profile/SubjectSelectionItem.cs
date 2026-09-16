using System;
using Avalonia.Media;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Models.Profile;

/// <summary>
/// 科目选择器中的一项。分组标题使用 <see cref="IsGroupHeader"/> 标记，不能被选中。
/// </summary>
public sealed class SubjectSelectionItem
{
    public SubjectSelectionItem(Guid key, Subject? value, string? groupName = null, string? groupColor = null,
        bool isUngroupedHeader = false)
    {
        // 分组标题不能参与科目 GUID 的选中匹配，使用独立键避免与未分组项冲突。
        Key = value == null ? Guid.NewGuid() : key;
        Value = value;
        GroupName = groupName;
        GroupColor = ParseGroupColor(groupColor);
        IsUngroupedHeader = isUngroupedHeader;
    }

    public Guid Key { get; }

    public Subject? Value { get; }

    public string? GroupName { get; }

    public Color? GroupColor { get; }

    public bool IsGroupHeader => Value == null;

    public bool IsUngroupedHeader { get; }

    public bool UsesDefaultGroupColor => IsGroupHeader && !IsUngroupedHeader && GroupColor == null;

    public bool HasCustomGroupColor => IsGroupHeader && GroupColor != null;

    private static Color? ParseGroupColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        try
        {
            return Color.Parse(color);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
