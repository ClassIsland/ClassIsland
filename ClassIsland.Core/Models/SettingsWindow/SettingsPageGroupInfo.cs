using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Models.SettingsWindow;

/// <summary>
/// 代表一个应用设置组信息。
/// </summary>
public class SettingsPageGroupInfo
{
    /// <summary>
    /// 分类图标
    /// </summary>
    public required string IconExpression { get; init; }

    /// <summary>
    /// 分类图标源
    /// </summary>
    public IconSource? IconSource => IconExpressionHelper.TryParseOrNull(IconExpression);
    
    /// <summary>
    /// 分类名称
    /// </summary>
    public required string Name { get; init; }
}