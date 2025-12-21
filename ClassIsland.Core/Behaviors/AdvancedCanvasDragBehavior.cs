// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//
// Modified by HelloWRC, 2025

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.Draggable;
using Avalonia.Xaml.Interactivity;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;


namespace ClassIsland.Core.Behaviors;

/// <summary>
/// Enables dragging of child controls within a <see cref="Canvas"/>.
/// </summary>
public class AdvancedCanvasDragBehavior : StyledElementBehavior<Control>
{
    public static readonly StyledProperty<bool> AllowDragFromDragThumbOnlyProperty = AvaloniaProperty.Register<AdvancedCanvasDragBehavior, bool>(
        nameof(AllowDragFromDragThumbOnly));

    public bool AllowDragFromDragThumbOnly
    {
        get => GetValue(AllowDragFromDragThumbOnlyProperty);
        set => SetValue(AllowDragFromDragThumbOnlyProperty, value);
    }
    
    private bool _enableDrag;
    private Point _start;
    private Control? _parent;
    private Control? _draggedContainer;
    private Control? _adorner;
    private bool _captured;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, Released, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, Pressed, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerMovedEvent, Moved, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerCaptureLostEvent, CaptureLost, RoutingStrategies.Tunnel);
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, Released);
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, Pressed);
            AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, Moved);
            AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, CaptureLost);
        }
    }

    private void AddAdorner(Control control)
    {
        var layer = AdornerLayer.GetAdornerLayer(control);
        if (layer is null)
        {
            return;
        }

        _adorner = new SelectionAdorner()
        {
            [AdornerLayer.AdornedElementProperty] = control
        };

        ((ISetLogicalParent) _adorner).SetParent(control);
        layer.Children.Add(_adorner);
    }

    private void RemoveAdorner(Control control)
    {
        var layer = AdornerLayer.GetAdornerLayer(control);
        if (layer is null || _adorner is null)
        {
            return;
        }

        layer.Children.Remove(_adorner);
        ((ISetLogicalParent) _adorner).SetParent(null);
        _adorner = null;
    }

    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        var isFromDragThumb = (e.Source as Control)?.FindAncestorOfType<TouchDragThumb>() is not null;
        var isFromCurrentThumb = e.Source is Control c1 && UITreeHelper.HasParent(c1, AssociatedObject);
        if ((e.Source as Control)?.FindAncestorOfType<ISelectable>() is {} selectable && !Equals(selectable, AssociatedObject))
        {
            isFromCurrentThumb = false;
        }
        var isTouchMode = e.Pointer.Type == PointerType.Touch;
        // AssociatedObject?.ShowToast($"(debug) {isFromDragThumb} {isFromCurrentThumb} {isTouchMode}");
        if (((isTouchMode || AllowDragFromDragThumbOnly) && !isFromDragThumb) || (isFromDragThumb && !isFromCurrentThumb))
        {
            return;
        }
        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (properties.IsLeftButtonPressed 
            && AssociatedObject?.Parent is Control parent && IsEnabled)
        {
            _enableDrag = true;
            _start = e.GetPosition(parent);
            _parent = parent;
            _draggedContainer = AssociatedObject;

            SetDraggingPseudoClasses(_draggedContainer, true);

            // AddAdorner(_draggedContainer);

            _captured = true;
        }
    }

    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (_captured)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                Released();
            }

            _captured = false;
        }
    }

    private void CaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        Released();
        _captured = false;
    }

    private void Moved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (_captured
            && properties.IsLeftButtonPressed && IsEnabled)
        {
            if (_parent is null || _draggedContainer is null || !_enableDrag)
            {
                return;
            }

            var position = e.GetPosition(_parent);
            var deltaX = position.X - _start.X;
            var deltaY = position.Y - _start.Y;
            _start = position;
            var left = Canvas.GetLeft(_draggedContainer);
            var top = Canvas.GetTop(_draggedContainer);
            Canvas.SetLeft(_draggedContainer, left + deltaX);
            Canvas.SetTop(_draggedContainer, top + deltaY);
        }
    }

    private void Released()
    {
        if (_enableDrag)
        {
            if (_parent is not null && _draggedContainer is not null)
            {
                // RemoveAdorner(_draggedContainer);
            }

            if (_draggedContainer is not null)
            {
                SetDraggingPseudoClasses(_draggedContainer, false);
            }

            _enableDrag = false;
            _parent = null;
            _draggedContainer = null;
        }
    }

    private void SetDraggingPseudoClasses(Control control, bool isDragging)
    {
        if (isDragging)
        {
            ((IPseudoClasses)control.Classes).Add(":dragging");
        }
        else
        {
            ((IPseudoClasses)control.Classes).Remove(":dragging");
        }
    }
}