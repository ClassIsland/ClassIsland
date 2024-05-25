using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using ClassIsland.Controls;

using Microsoft.Xaml.Behaviors;

namespace ClassIsland.Behaviors;

public class TimeLineListItemBehavior : Behavior<ListBoxItem>
{
    public static readonly DependencyProperty ThumbTemplateProperty = DependencyProperty.Register(
        nameof(ThumbTemplate), typeof(ControlTemplate), typeof(TimeLineListItemBehavior), new PropertyMetadata(default(ControlTemplate)));

    public ControlTemplate ThumbTemplate
    {
        get { return (ControlTemplate)GetValue(ThumbTemplateProperty); }
        set { SetValue(ThumbTemplateProperty, value); }
    }

    private Adorner? _adorner;

    protected override void OnAttached()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.Selected += AssociatedObjectOnSelected;
            AssociatedObject.Unselected += AssociatedObjectOnUnselected;
        }
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        RemoveAdorner();
        base.OnDetaching();
    }

    private void AssociatedObjectOnUnselected(object sender, RoutedEventArgs e)
    {
        RemoveAdorner();
    }

    private void RemoveAdorner()
    {
        if (AssociatedObject == null)
        {
            return;
        }
        if (_adorner != null)
        {
            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            layer?.Remove(_adorner);
            _adorner = null;
        }
    }

    private void AssociatedObjectOnSelected(object sender, RoutedEventArgs e)
    {
        if (AssociatedObject == null)
        {
            return;
        }
        var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
        if (_adorner != null)
        {
            return;
        }
        _adorner = new TimeLineListItemAdorner(AssociatedObject, ThumbTemplate);
        layer?.Add(_adorner);
    }
}