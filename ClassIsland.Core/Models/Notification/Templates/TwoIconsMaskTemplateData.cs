using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Models.Notification.Templates;

/// <summary>
/// 双图标遮罩模板数据。
/// </summary>
public partial class TwoIconsMaskTemplateData : ObservableObject
{
    /// <summary>
    /// 左侧图标类型
    /// </summary>
    [ObservableProperty] private PackIconKind _leftIconKind = PackIconKind.AlertCircleOutline;

    /// <summary>
    /// 右侧图标类型
    /// </summary>
    [ObservableProperty] private PackIconKind _rightIconKind = PackIconKind.BellRing;

    /// <summary>
    /// 是否拥有右侧图标
    /// </summary>
    [ObservableProperty] private bool _hasRightIcon = true;

    /// <summary>
    /// 遮罩文本
    /// </summary>
    [ObservableProperty] private string _text = "";

    /// <summary>
    /// 模板资源键
    /// </summary>
    public const string TemplateResourceKey = "NotificationTwoIconsMaskTemplate";
}