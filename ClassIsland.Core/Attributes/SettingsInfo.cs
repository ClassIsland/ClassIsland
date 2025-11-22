namespace ClassIsland.Core.Attributes;

/// <summary>
/// 应用设置属性信息。<br/>
/// 用于：行动“应用设置”中用户选择应用设置。
/// </summary>
/// <param name="name">应用设置属性中文名称。</param>
/// <param name="glyph">应用设置属性图标。</param>
/// <param name="enums">应用设置属性枚举项中文名称。</param>
/// <param name="order">应用设置属性排序顺序。数字越大排在越后。默认值为 10。</param>
[AttributeUsage(AttributeTargets.Property)]
public class SettingsInfo(
    string? name = null,
    string? glyph = null,
    string[]? enums = null,
    double order = 10
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

    /// <summary>
    /// 应用设置属性排序顺序。数字越大排在越后。
    /// </summary>
    public double Order { get; set; } = order;
}