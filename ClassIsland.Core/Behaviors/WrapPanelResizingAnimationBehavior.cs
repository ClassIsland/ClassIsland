using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// <see cref="WrapPanel"/> 元素大小改变动画过渡行为
/// </summary>
public class WrapPanelResizingAnimationBehavior
{
    public static readonly AttachedProperty<bool> IsResizingAnimationEnabledProperty =
        AvaloniaProperty.RegisterAttached<WrapPanelResizingAnimationBehavior, Panel, bool>("IsResizingAnimationEnabled");

    public static void SetIsResizingAnimationEnabled(Panel obj, bool value) => obj.SetValue(IsResizingAnimationEnabledProperty, value);
    public static bool GetIsResizingAnimationEnabled(Panel obj) => obj.GetValue(IsResizingAnimationEnabledProperty);

    public static readonly AttachedProperty<bool> IsAnimationAttachedProperty =
        AvaloniaProperty.RegisterAttached<WrapPanelResizingAnimationBehavior, Visual, bool>("IsAnimationAttached");

    private static void SetIsAnimationAttached(Visual obj, bool value) => obj.SetValue(IsAnimationAttachedProperty, value);
    public static bool GetIsAnimationAttached(Visual obj) => obj.GetValue(IsAnimationAttachedProperty);
    
    static WrapPanelResizingAnimationBehavior()
    {
        IsResizingAnimationEnabledProperty.Changed.AddClassHandler<Panel>(HandleIsResizingAnimationEnabledChanged);
    }

    public static readonly AttachedProperty<bool> IsLoadedHandlerAttachedProperty =
        AvaloniaProperty.RegisterAttached<WrapPanelResizingAnimationBehavior, Control, bool>("IsLoadedHandlerAttached");

    private static void SetIsLoadedHandlerAttached(Control obj, bool value) => obj.SetValue(IsLoadedHandlerAttachedProperty, value);
    public static bool GetIsLoadedHandlerAttached(Control obj) => obj.GetValue(IsLoadedHandlerAttachedProperty);
    
    private static void HandleIsResizingAnimationEnabledChanged(Panel panel, AvaloniaPropertyChangedEventArgs args)
    {
        if (IThemeService.AnimationLevel < 2)
        {
            return;
        }
        var enabled = GetIsResizingAnimationEnabled(panel);
        if (enabled)
        {
            foreach (var child in panel.Children.OfType<Control>())
            {
                TrySetupControl(child);
            }
            panel.Children.CollectionChanged += ChildrenOnCollectionChanged;
        }
        else
        {
            foreach (var child in panel.Children.OfType<Control>())
            {
                ClearControlAnimation(child);
            }
            panel.Children.CollectionChanged -= ChildrenOnCollectionChanged;
        }
    }

    private static void TrySetupControl(Control control)
    {
        if (!control.IsLoaded)
        {
            control.Loaded += ChildOnLoaded;
            return;
        }
        SetupControlAnimation(control);
    }

    private static void ControlOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }
        TrySetupControl(control);
    }

    private static void ChildOnLoaded(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (sender is not Control control)
        {
            return;
        }
        SetupControlAnimation(control);
        control.Loaded -= ChildOnLoaded;
    }

    private static void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.NewItems?.OfType<Control>() ?? new List<Control>())
        {
            TrySetupControl(item);
        }
        foreach (var item in e.OldItems?.OfType<Control>() ?? new List<Control>())
        {
            ClearControlAnimation(item);
        }
    }

    private static void SetupControlAnimation(Control control)
    {
        if (!GetIsLoadedHandlerAttached(control))
        {
            control.AttachedToVisualTree += ControlOnAttachedToVisualTree;
            control.DetachedFromVisualTree += ControlOnDetachedFromVisualTree;
            SetIsLoadedHandlerAttached(control, true);
        }
        var compositionVisual = ElementComposition.GetElementVisual(control);
        if (compositionVisual == null)
        {
            return;
        }
        var compositor = compositionVisual.Compositor;
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Target = nameof(compositionVisual.Offset);
        offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", new CubicEaseInOut());
        offsetAnimation.Duration = TimeSpan.FromMilliseconds(225);
        var implicitAnimationCollection = compositor.CreateImplicitAnimationCollection();
        implicitAnimationCollection[nameof(compositionVisual.Offset)] = offsetAnimation;
        compositionVisual.ImplicitAnimations = implicitAnimationCollection;
        SetIsAnimationAttached(control, true);
    }

    private static void ControlOnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }
        
        ClearControlAnimation(control);
    }

    private static void ClearControlAnimation(Control control)
    {
        var compositionVisual = ElementComposition.GetElementVisual(control);
        if (compositionVisual == null)
        {
            return;
        }
        var compositor = compositionVisual.Compositor;
        compositionVisual.ImplicitAnimations = compositor.CreateImplicitAnimationCollection();
        SetIsAnimationAttached(control, false);
    }
}