namespace ClassIsland.Core.Attributes;

/// <summary>
/// 应用设置属性信息。<br/>
/// 用于：行动“应用设置”中用户选择应用设置。
/// </summary>
/// <param name="name">应用设置属性中文名称。</param>
/// <param name="glyph">应用设置属性图标。</param>
/// <param name="enums">应用设置属性枚举项中文名称。</param>
[AttributeUsage(AttributeTargets.Property)]
public class SettingsInfo(
    string? name = null,
    string? glyph = null,
    string[]? enums = null
) : Attribute
{
    /// <summary>
    /// 应用设置属性中文名称。
    /// </summary>
    public string? Name { get; set; } = name;

    /// <summary>
    /// 应用设置属性图标。
    /// </summary>
    public string? Glyph { get; set; } = glyph;

    /// <summary>
    /// 应用设置属性枚举项中文名称。
    /// </summary>
    public string[]? Enums { get; set; } = enums;
}