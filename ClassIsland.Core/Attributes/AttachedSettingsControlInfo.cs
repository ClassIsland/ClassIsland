using System.Diagnostics.CodeAnalysis;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Attributes;

/// <summary>
/// 附加设置信息
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AttachedSettingsControlInfo(
    string guid,
    string name,
    string iconExpression = "\uef27",
    bool hasEnabledState = true) : Attribute
{
    private readonly string _iconExpression = iconExpression;

    /// <summary>
    /// 附加设置 GUID
    /// </summary>
    public Guid Guid { get; } = new Guid(guid);

    /// <summary>
    /// 附加设置控件名称
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 附加设置图标。注册信息由静态集合长期保存，因此每次读取都返回独立图标源，
    /// 避免图标源通过其生成的图标元素保留设置控件所在的视觉树。
    /// </summary>
    public FAIconSource? IconSource => IconExpressionHelper.TryParseOrNull(_iconExpression);

    /// <summary>
    /// 是否具有开关状态
    /// </summary>
    public bool HasEnabledState { get; } = hasEnabledState;

    /// <summary>
    /// 附加设置控件类型
    /// </summary>
    public Type AttachedSettingsControlType { get; internal set; } = null!;

    /// <summary>
    /// 附加设置可以附加的目标。
    /// </summary>
    public AttachedSettingsTargets Targets { get; internal set; } = AttachedSettingsTargets.None;
}
