using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification.Templates;

/// <summary>
/// 代表简单文本模板的数据。
/// </summary>
public partial class SimpleTextTemplateData : ObservableObject
{
    /// <summary>
    /// 要显示的文本
    /// </summary>
    [ObservableProperty] private string? _text;

    /// <summary>
    /// 模板资源键
    /// </summary>
    public const string TemplateResourceKey = "NotificationSimpleTextOverlayTemplate";
}