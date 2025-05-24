using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core;
using System.Windows.Documents;
using Microsoft.Xaml.Behaviors;

namespace ClassIsland.Behaviors;

public class AdornerAttachingBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty AdornerTemplateProperty = DependencyProperty.Register(
        nameof(AdornerTemplate), typeof(ControlTemplate), typeof(AdornerAttachingBehavior), new PropertyMetadata(default(ControlTemplate)));

    public ControlTemplate AdornerTemplate
    {
        get { return (ControlTemplate)GetValue(AdornerTemplateProperty); }
        set { SetValue(AdornerTemplateProperty, value); }
    }

    public static readonly DependencyProperty AdornerDataContextProperty = DependencyProperty.Register(
        nameof(AdornerDataContext), typeof(object), typeof(AdornerAttachingBehavior), new PropertyMetadata(default(object?)));

    public object? AdornerDataContext
    {
        get { return (object?)GetValue(AdornerDataContextProperty); }
        set { SetValue(AdornerDataContextProperty, value); }
    }

    private Adorner? _adorner;

    protected override void OnAttached()
    {
        if (AssociatedObject != null)
        {
            AddAdorner();
        }
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        RemoveAdorner();
        base.OnDetaching();
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

    private void AddAdorner()
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
        _adorner = new TimeLineListItemAdorner(AssociatedObject, AdornerTemplate)
        {
            DataContext = AdornerDataContext
        };
        
        layer?.Add(_adorner);
    }
}