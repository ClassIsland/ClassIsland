using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ClassIsland.Controls;

public class TimeLineListItemAdorner : Adorner
{
    private VisualCollection _visualCollection;

    private Control _control ;

    public TimeLineListItemAdorner(UIElement adornedElement, ControlTemplate template) : base(adornedElement)
    {
        _visualCollection = new VisualCollection(this);
        _control = new Control()
        {
            Template = template,
            ClipToBounds = false
        };
        ClipToBounds = false;
        _visualCollection.Add(_control);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        
        base.OnRender(drawingContext);
    }

    protected override int VisualChildrenCount
    {
        get
        {
            return _visualCollection.Count;
        }
    }

    protected override Visual GetVisualChild(int index)
    {
        return _visualCollection[index];
    }

    protected override Size MeasureOverride(Size constraint)
    {
        return base.MeasureOverride(constraint);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _control.Arrange(new Rect(finalSize));

        return base.ArrangeOverride(finalSize);
    }
}