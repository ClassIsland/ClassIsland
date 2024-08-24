using ClassIsland.Core.Enums;

namespace ClassIsland.Core.Attributes;

/// <summary>
/// 附加设置控件用法。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AttachedSettingsUsage(AttachedSettingsTargets targets) : Attribute
{
    /// <summary>
    /// 用法。
    /// </summary>
    public AttachedSettingsTargets Targets { get; } = targets;
}