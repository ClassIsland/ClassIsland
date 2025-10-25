namespace ClassIsland.Core.Attributes;

/// <summary>
/// 自动化触发器信息。
/// </summary>
/// <param name="id">触发器 ID</param>
/// <param name="name">触发器名称</param>
/// <param name="iconGlyph">触发器图表类型</param>
[AttributeUsage(AttributeTargets.Class)]
public class TriggerInfo(string id, string name, string iconGlyph="\uED55") : Attribute
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
    /// 触发器图标类型
    /// </summary>
    public string IconGlyph { get; } = iconGlyph;

    /// <summary>
    /// 触发器类型
    /// </summary>
    public Type? TriggerType { get; internal set; }

    /// <summary>
    /// 设置界面类型
    /// </summary>
    public Type? SettingsControlType { get; internal set; }
}