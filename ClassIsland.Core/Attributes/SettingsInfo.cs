using static ClassIsland.Core.Attributes.SettingsInfo;
namespace ClassIsland.Core.Attributes;

/// <summary>
/// 应用设置属性信息。
/// </summary>
/// <param name="name">应用设置属性中文名称。</param>
/// <param name="category">应用设置属性分类。</param>
/// <param name="glyph">应用设置属性图标。</param>
/// <param name="note">应用设置属性备注。</param>
/// <param name="enums">应用设置属性枚举项中文名称。</param>
/// <param name="min">应用设置属性最小值。</param>
/// <param name="max">应用设置属性最大值。</param>
[AttributeUsage(AttributeTargets.Property)]
public class SettingsInfo(
    string name,
    SettingsInfoCategory category = SettingsInfoCategory.None,
    string? glyph = null,
    string? note = null,
    string[]? enums = null,
    double min = double.NaN,
    double max = double.NaN
) : Attribute
{
    /// <summary>
    /// 应用设置属性中文名称。
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 应用设置属性分类。
    /// </summary>
    public SettingsInfoCategory Category { get; } = category;

    /// <summary>
    /// 应用设置属性图标。
    /// </summary>
    public string? IconGlyph { get; } = glyph;

    /// <summary>
    /// 应用设置属性备注。
    /// </summary>
    public string? Note { get; } = note;

    /// <summary>
    /// 应用设置属性枚举项中文名称。
    /// </summary>
    public string[]? Enums { get; } = enums;

    /// <summary>
    /// 应用设置属性最小值。
    /// </summary>
    public double? Minimum { get; } = min is double.NaN ? null : min;

    /// <summary>
    /// 应用设置属性最大值。
    /// </summary>
    public double? Maximum { get; } = max is double.NaN ? null : max;

    /// <summary>
    /// 应用设置属性分类。
    /// </summary>
    public enum SettingsInfoCategory
    {
        /// <summary>
        /// 未分类。
        /// </summary>
        None,

        /// <summary>
        /// 主界面。
        /// </summary>
        MainWindow,
    }
}