namespace ClassIsland.Core.Attributes;

/// <summary>
/// 行动提供方信息。
/// </summary>
/// <param name="id">行动提供方 ID。形如 "classisland.settings"。</param>
/// <param name="name">行动提供方名称。</param>
/// <param name="iconGlyph">行动提供方图标。形如 "\uec2e" 的 FluentIcon Glyph 格式。支持留空。</param>
/// <param name="isRevertable">行动提供方是否支持恢复。默认为 false。</param>
[AttributeUsage(AttributeTargets.Class)]
public class ActionInfo(string id, string name, string? iconGlyph = null, bool isRevertable = false) : Attribute
{
    /// <summary>
    /// 行动提供方 ID。形如 "classisland.settings"。
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// 行动提供方名称。
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 行动提供方图标。形如 "\uec2e" 的 FluentIcon Glyph 格式。支持留空。
    /// </summary>
    public string? IconGlyph { get; } = iconGlyph;

    /// <summary>
    /// 行动提供方是否支持恢复。
    /// </summary>
    public bool IsRevertable { get; } = isRevertable;
}