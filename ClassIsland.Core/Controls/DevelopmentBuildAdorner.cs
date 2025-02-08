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
    /// <inheritdoc />
    public DevelopmentBuildAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _visualCollection = new VisualCollection(this);
        _control = new Control()
        {
            Template = FindResource("DevelopmentBuildAdornerControlTemplate") as ControlTemplate,
            ClipToBounds = false
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