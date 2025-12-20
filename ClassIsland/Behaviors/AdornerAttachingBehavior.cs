using System.Windows;
using ClassIsland.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Xaml.Interactivity;

namespace ClassIsland.Behaviors;

public class AdornerAttachingBehavior : StyledElementBehavior<Control>
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
    private AdornerLayer? _layer;

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
        var owner = AssociatedObject;
        if (owner == null )
        {
            return;
        }
        if (_adorner != null)
        {
            _layer?.Children.Remove(_adorner);
            AdornerLayer.SetAdornedElement(_adorner, null);
            _adorner = null;
        }
    }

    private void AddAdorner()
    {
        if (AssociatedObject == null)
        {
            return;
        }
        var layer = _layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
        if (layer == null || _adorner != null)
        {
            return;
        }
        _adorner = new TemplatedControl
        {
            DataContext = AdornerDataContext ?? DataContext,
            ClipToBounds = false,
            Template = AdornerTemplate
        };
        
        AdornerLayer.SetIsClipEnabled(_adorner, false);
        layer?.Children.Add(_adorner);
        AdornerLayer.SetAdornedElement(_adorner, AssociatedObject);
    }
}