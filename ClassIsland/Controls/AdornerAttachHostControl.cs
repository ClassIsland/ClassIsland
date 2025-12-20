using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using ClassIsland.Behaviors;

namespace ClassIsland.Controls;

public class AdornerAttachHostControl : ContentControl
{
    public static readonly StyledProperty<ControlTemplate> AdornerTemplateProperty = AvaloniaProperty.Register<AdornerAttachHostControl, ControlTemplate>(
        nameof(AdornerTemplate));
    public ControlTemplate AdornerTemplate
    {
        get => GetValue(AdornerTemplateProperty);
        set => SetValue(AdornerTemplateProperty, value);
    
    }

    public static readonly StyledProperty<object> AdornerDataContextProperty = AvaloniaProperty.Register<AdornerAttachHostControl, object>(
        nameof(AdornerDataContext));
    public object AdornerDataContext
    {
        get => GetValue(AdornerDataContextProperty);
        set => SetValue(AdornerDataContextProperty, value);
    
    }

    public static readonly StyledProperty<bool> IsAttachedProperty = AvaloniaProperty.Register<AdornerAttachHostControl, bool>(
        nameof(IsAttached));

    public bool IsAttached
    {
        get => GetValue(IsAttachedProperty);
        set => SetValue(IsAttachedProperty, value);
    }

    private Control? _adorner;
    private AdornerLayer? _layer;
    private IDisposable? _isAttachedObserver;
    
    
    public AdornerAttachHostControl()
    {
        
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        _isAttachedObserver?.Dispose();
        _isAttachedObserver = this.GetObservable(IsAttachedProperty).Subscribe(_ =>
        {
            if (IsAttached)
            {
                AddAdorner();
            }
            else
            {
                RemoveAdorner();
            }
        });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        _isAttachedObserver?.Dispose();
        RemoveAdorner();
    }


    private void RemoveAdorner()
    {
        var owner = this;
        if (_adorner != null)
        {
            _layer?.Children.Remove(_adorner);
            AdornerLayer.SetAdornedElement(_adorner, null);
            _adorner = null;
        }
    }

    private void AddAdorner()
    {
        var layer = _layer = AdornerLayer.GetAdornerLayer(this);
        if (layer == null || _adorner != null)
        {
            return;
        }
        _adorner = new TemplatedControl
        {
            DataContext = DataContext,
            ClipToBounds = false,
            Template = AdornerTemplate
        };
        
        AdornerLayer.SetIsClipEnabled(_adorner, false);
        layer?.Children.Add(_adorner);
        AdornerLayer.SetAdornedElement(_adorner, this);
    }
}