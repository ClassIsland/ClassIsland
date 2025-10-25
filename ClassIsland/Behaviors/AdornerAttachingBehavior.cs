using System.Windows;
using ClassIsland.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Xaml.Interactivity;

namespace ClassIsland.Behaviors;

public class AdornerAttachingBehavior : Behavior<Control>
{
    public static readonly StyledProperty<ControlTemplate> AdornerTemplateProperty = AvaloniaProperty.Register<AdornerAttachingBehavior, ControlTemplate>(
        nameof(AdornerTemplate));
    public ControlTemplate AdornerTemplate
    {
        get => GetValue(AdornerTemplateProperty);
        set => SetValue(AdornerTemplateProperty, value);
    
    }

    public static readonly StyledProperty<object> AdornerDataContextProperty = AvaloniaProperty.Register<AdornerAttachingBehavior, object>(
        nameof(AdornerDataContext));
    public object AdornerDataContext
    {
        get => GetValue(AdornerDataContextProperty);
        set => SetValue(AdornerDataContextProperty, value);
    
    }

    private Control? _adorner;

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
            AdornerLayer.SetAdorner(AssociatedObject, null);
            layer?.Children.Remove(_adorner);
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
        _adorner = new TemplatedControl
        {
            DataContext = AdornerDataContext,
            Template = AdornerTemplate
        };
        
        layer?.Children.Add(_adorner);
        AdornerLayer.SetAdorner(AssociatedObject, _adorner);
    }
}