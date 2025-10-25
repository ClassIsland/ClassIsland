using System.ComponentModel;
using Avalonia.Media;

namespace ClassIsland.Core.Abstractions.Models.Components;

/// <summary>
/// 一个代表主界面可自定义节点设置
/// </summary>
public interface IMainWindowCustomizableNodeSettings : INotifyPropertyChanged
{
    /// <summary>
    /// 是否启用资源覆盖
    /// </summary>
    public bool IsResourceOverridingEnabled { get; set; }

    /// <summary>
    /// 次级字体大小
    /// </summary>
    public double MainWindowSecondaryFontSize { get; set; }

    /// <summary>
    /// 正文字体大小
    /// </summary>
    public double MainWindowBodyFontSize { get; set; }

    /// <summary>
    /// 强调字体大小
    /// </summary>
    public double MainWindowEmphasizedFontSize { get; set; }

    /// <summary>
    /// 大号字体大小
    /// </summary>
    public double MainWindowLargeFontSize { get; set; }

    /// <summary>
    /// 是否启用自定义前景色
    /// </summary>
    public bool IsCustomForegroundColorEnabled { get; set; }

    /// <summary>
    /// 自定义前景色
    /// </summary>
    public Color ForegroundColor { get; set; }
    
    /// <summary>
    /// 自定义背景不透明度
    /// </summary>
    public double BackgroundOpacity { get; set; }
    
    /// <summary>
    /// 是否启用自定义背景不透明度
    /// </summary>
    public bool IsCustomBackgroundOpacityEnabled { get; set; }
    
    /// <summary>
    /// 自定义背景色
    /// </summary>
    public Color BackgroundColor { get; set; }
    
    /// <summary>
    /// 是否启用自定义背景色
    /// </summary>
    public bool IsCustomBackgroundColorEnabled { get; set; }

    /// <summary>
    /// 自定义圆角半径
    /// </summary>
    public double CustomCornerRadius { get; set; }
    
    /// <summary>
    /// 是否启用自定义圆角半径
    /// </summary>
    public bool IsCustomCornerRadiusEnabled { get; set; }
    
    /// <summary>
    /// 元素不透明度
    /// </summary>
    public double Opacity { get; set; }
}