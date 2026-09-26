namespace ClassIsland.Core.Attributes;

using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

/// <summary>
/// 代表提醒渠道信息
/// </summary>
/// <inheritdoc />
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NotificationChannelInfo(string guid, string name, string description = "", Type? settingsControlType = null) : Attribute
{
    /// <summary>
    /// 渠道 GUID
    /// </summary>
    public Guid Guid { get; } = Guid.Parse(guid);

    /// <summary>
    /// 渠道图标
    /// </summary>
    public FAIconSource? IconSource { get; } =
        IconExpressionHelper.TryParseOrNull("\ue02a");

    /// <summary>
    /// 渠道名称
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 渠道描述
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// 设置界面类型
    /// </summary>
    public Type? SettingsControlType { get; } = settingsControlType;

    /// <summary>
    /// 提醒渠道所属提醒提供方的 GUID
    /// </summary>
    public Guid AssociatedProviderGuid { get; internal set; } = Guid.Empty;

    /// <summary>
    /// 初始化一个 <see cref="NotificationChannelInfo"/> 实例
    /// </summary>
    public NotificationChannelInfo(string guid, string name, string iconExpression, string description = "", Type? settingsControlType = null) : this(guid, name,
        description, settingsControlType)
    {
        IconSource = IconExpressionHelper.TryParseOrNull(iconExpression);
    }
}
