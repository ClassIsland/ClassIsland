using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 开发构建装饰层
/// </summary>
public class DevelopmentBuildAdorner : TemplatedControl
{
    /// <summary>
    /// 是否是开发构建
    /// </summary>
    public bool IsDevelopmentBuild { get; }

    /// <summary>
    /// 是否显示开源水印
    /// </summary>
    public bool ShowOssWatermark { get; }

    /// <inheritdoc />
    public DevelopmentBuildAdorner(bool isDevelopmentBuild, bool showOssWatermark)
    {
        IsDevelopmentBuild = isDevelopmentBuild;
        ShowOssWatermark = showOssWatermark;
        ClipToBounds = false;
    }
}