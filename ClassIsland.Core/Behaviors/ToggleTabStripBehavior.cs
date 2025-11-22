using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// 
/// </summary>
public class ToggleTabStripBehavior : Behavior<TabStrip>
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PseudoClasses")]
    private static extern IPseudoClasses GetPseudoClasses(StyledElement element);
    
    protected override void OnAttached()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.PointerPressed += AssociatedObjectOnPointerPressed;
            AssociatedObject.PointerReleased += AssociatedObjectOnPointerReleased;
        }
        base.OnAttached();
    }

    private void AssociatedObjectOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Handled = true;
        if (AssociatedObject != null)
        {
            var ni = AssociatedObject.SelectedIndex + 1;
            AssociatedObject.SelectedIndex = ni >= AssociatedObject.ItemsView.Count ? 0 : ni;
            GetPseudoClasses(AssociatedObject).Set(":pointerdown", false);
        }
    }

    private void AssociatedObjectOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (AssociatedObject != null)
        {
            GetPseudoClasses(AssociatedObject).Set(":pointerdown", true);
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            GetPseudoClasses(AssociatedObject).Set(":pointerdown", false);
            AssociatedObject.PointerPressed -= AssociatedObjectOnPointerPressed;
            AssociatedObject.PointerReleased -= AssociatedObjectOnPointerReleased;
        }
        base.OnDetaching();
    }
}