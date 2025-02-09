using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 开发构建装饰层
/// </summary>
public class DevelopmentBuildAdorner : Adorner
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
    public DevelopmentBuildAdorner(UIElement adornedElement, bool isDevelopmentBuild, bool showOssWatermark) : base(adornedElement)
    {
        IsDevelopmentBuild = isDevelopmentBuild;
        ShowOssWatermark = showOssWatermark;
        _visualCollection = new VisualCollection(this);
        _control = new Control()
        {
            Template = FindResource("DevelopmentBuildAdornerControlTemplate") as ControlTemplate,
            ClipToBounds = false,
            DataContext = this,
            IsTabStop = false
        };
        ClipToBounds = false;
        _visualCollection.Add(_control);
    }

    private readonly VisualCollection _visualCollection;

    private readonly Control _control;


    /// <inheritdoc />
    protected override int VisualChildrenCount => _visualCollection.Count;

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index)
    {
        return _visualCollection[index];
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _control.Arrange(new Rect(finalSize));

        return base.ArrangeOverride(finalSize);
    }
}