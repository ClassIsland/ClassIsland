using Avalonia.Controls;
using ClassIsland.Core.Enums.Profile;
using ClassIsland.Core.Helpers.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Models.Profile;

/// <summary>
/// 代表档案数据迁移提供方信息
/// </summary>
public class ProfileTransferProviderInfo
{
    internal ProfileTransferProviderInfo() { }
    
    /// <summary>
    /// 提供方类型
    /// </summary>
    public required ProfileTransferProviderType Type { get; init; }
    
    /// <summary>
    /// 提供方 ID
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// 提供方名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 提供方图标
    /// </summary>
    public IconSource? Icon { get; init; } = IconExpressionHelper.Parse("\ue68f");

    /// <summary>
    /// 提供方处理器
    /// </summary>
    public Action<TopLevel>? FunctionHandler { get; init; }
    
    /// <summary>
    /// 提供方控件
    /// </summary>
    public Type? HandlerControlType { get; init; }
    
    /// <summary>
    /// 提供方控件是否是全宽度控件
    /// </summary>
    public bool UseFullWidth { get; init; }
    
    /// <summary>
    /// 提供方控件是否隐藏标题
    /// </summary>
    public bool HidePageTitle { get; init; }
}