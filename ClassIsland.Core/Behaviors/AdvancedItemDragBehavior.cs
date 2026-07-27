// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//
// Modified by HelloWRC, 2025

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// Allows dragging items within an <see cref="ItemsControl"/>.
/// </summary>
public class AdvancedItemDragBehavior : StyledElementBehavior<Control>
{
    private bool _enableDrag;
    private bool _dragStarted;
    private Point _start;
    private int _draggedIndex;
    private int _targetIndex;
    private ItemsControl? _itemsControl;
    private Control? _draggedContainer;
    private bool _captured;
    private Control? _pendingReattachContainer;
    private ItemsControl? _ownerItemsControl;

    /// <summary>
    /// Identifies the <see cref="Orientation"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<AdvancedItemDragBehavior, Orientation>(nameof(Orientation));

    /// <summary>
    /// Identifies the <see cref="HorizontalDragThreshold"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> HorizontalDragThresholdProperty =
        AvaloniaProperty.Register<AdvancedItemDragBehavior, double>(nameof(HorizontalDragThreshold), 3);

    /// <summary>
    /// Identifies the <see cref="VerticalDragThreshold"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> VerticalDragThresholdProperty =
        AvaloniaProperty.Register<AdvancedItemDragBehavior, double>(nameof(VerticalDragThreshold), 3);

    /// <summary>
    /// Gets or sets the orientation of the drag operation.
    /// </summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Gets or sets the horizontal drag threshold in pixels.
    /// </summary>
    public double HorizontalDragThreshold
    {
        get => GetValue(HorizontalDragThresholdProperty);
        set => SetValue(HorizontalDragThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the vertical drag threshold in pixels.
    /// </summary>
    public double VerticalDragThreshold
    {
        get => GetValue(VerticalDragThresholdProperty);
        set => SetValue(VerticalDragThresholdProperty, value);
    }

    public static readonly StyledProperty<bool> AllowDragFromDragThumbOnlyProperty = AvaloniaProperty.Register<AdvancedItemDragBehavior, bool>(
        nameof(AllowDragFromDragThumbOnly));

    public bool AllowDragFromDragThumbOnly
    {
        get => GetValue(AllowDragFromDragThumbOnlyProperty);
        set => SetValue(AllowDragFromDragThumbOnlyProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        StopWatchingForContainerReuse();
        _ownerItemsControl = AssociatedObject?.Parent as ItemsControl ??
                             (AssociatedObject is not null
                                 ? ItemsControl.ItemsControlFromItemContainer(AssociatedObject)
                                 : null);

        if (AssociatedObject is not null)
        {
            AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, PointerReleased, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, PointerPressed, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerMovedEvent, PointerMoved, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerCaptureLostEvent, PointerCaptureLost, RoutingStrategies.Tunnel);
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            if (_ownerItemsControl is not null &&
                SupportsContainerContent(AssociatedObject) &&
                GetContainerContent(AssociatedObject) is null)
            {
                // Virtualized item containers are cleared and reused without necessarily
                // leaving the visual tree, so wait for this container to receive new content.
                WatchForContainerReuse(AssociatedObject);
            }

            AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, PointerReleased);
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, PointerPressed);
            AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, PointerMoved);
            AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, PointerCaptureLost);
        }

        base.OnDetaching();
    }

    private void PointerPressed(object? sender, PointerPressedEventArgs e)
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
            && AssociatedObject?.Parent is ItemsControl itemsControl)
        {
            _enableDrag = true;
            _dragStarted = false;
            _start = e.GetPosition(itemsControl);
            _draggedIndex = -1;
            _targetIndex = -1;
            _itemsControl = itemsControl;
            _draggedContainer = AssociatedObject;

            if (_draggedContainer is not null)
            {
                SetDraggingPseudoClasses(_draggedContainer, true);
            }

            AddTransforms(_itemsControl);

            _captured = true;
        }
    }

    private void PointerReleased(object? sender, PointerReleasedEventArgs e)
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

    private void PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        Released();
        _captured = false;
    }

    private void Released()
    {
        if (!_enableDrag)
        {
            return;
        }

        RemoveTransforms(_itemsControl);

        if (_itemsControl is not null)
        {
            foreach (var control in _itemsControl.GetRealizedContainers())
            {
                SetDraggingPseudoClasses(control, true);
            }
        }

        if (_dragStarted)
        {
            if (_draggedIndex >= 0 && _targetIndex >= 0 && _draggedIndex != _targetIndex)
            {
                MoveDraggedItem(_itemsControl, _draggedIndex, _targetIndex);
            }
        }

        if (_itemsControl is not null)
        {
            foreach (var control in _itemsControl.GetRealizedContainers())
            {
                SetDraggingPseudoClasses(control, false);
            }
        }

        if (_draggedContainer is not null)
        {
            SetDraggingPseudoClasses(_draggedContainer, false);
        }

        _draggedIndex = -1;
        _targetIndex = -1;
        _enableDrag = false;
        _dragStarted = false;
        _itemsControl = null;

        _draggedContainer = null;
    }

    private void AddTransforms(ItemsControl? itemsControl)
    {
        if (itemsControl?.Items is null)
        {
            return;
        }

        var i = 0;

        foreach (var _ in itemsControl.Items)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is not null)
            {
                SetTranslateTransform(container, 0, 0);
            }
  
            i++;
        }  
    }

    private void RemoveTransforms(ItemsControl? itemsControl)
    {
        if (itemsControl?.Items is null)
        {
            return;
        }

        var i = 0;

        foreach (var _ in itemsControl.Items)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container is not null)
            {
                SetTranslateTransform(container, 0, 0);
            }
  
            i++;
        }  
    }

    private void MoveDraggedItem(ItemsControl? itemsControl, int draggedIndex, int targetIndex)
    {
        if (itemsControl?.ItemsSource is IList itemsSource)
        {
            var draggedItem = itemsSource[draggedIndex];
            itemsSource.RemoveAt(draggedIndex);
            itemsSource.Insert(targetIndex, draggedItem);

            if (itemsControl is SelectingItemsControl selectingItemsControl)
            {
                selectingItemsControl.SelectedIndex = targetIndex;
            }
        }
        else
        {
            if (itemsControl?.Items is {IsReadOnly: false} itemCollection)
            {
                var draggedItem = itemCollection[draggedIndex];
                itemCollection.RemoveAt(draggedIndex);
                itemCollection.Insert(targetIndex, draggedItem);

                if (itemsControl is SelectingItemsControl selectingItemsControl)
                {
                    selectingItemsControl.SelectedIndex = targetIndex;
                } 
            }
        }
    }

    private void WatchForContainerReuse(Control control)
    {
        if (ReferenceEquals(_pendingReattachContainer, control))
        {
            return;
        }

        StopWatchingForContainerReuse();
        _pendingReattachContainer = control;
        control.PropertyChanged += ContainerOnPropertyChanged;
    }

    private void StopWatchingForContainerReuse()
    {
        if (_pendingReattachContainer is not null)
        {
            _pendingReattachContainer.PropertyChanged -= ContainerOnPropertyChanged;
            _pendingReattachContainer = null;
        }
    }

    private void ContainerOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Control control ||
            !IsContainerContentProperty(control, e.Property) ||
            GetContainerContent(control) is null)
        {
            return;
        }

        StopWatchingForContainerReuse();
        Dispatcher.UIThread.Post(() => RestoreBehavior(control));
    }

    private void RestoreBehavior(Control control)
    {
        if (AssociatedObject is not null ||
            _ownerItemsControl is null ||
            _ownerItemsControl.IndexFromContainer(control) < 0 ||
            !control.IsSet(Interaction.BehaviorsProperty))
        {
            return;
        }

        var behaviors = Interaction.GetBehaviors(control);
        if (behaviors.Any(x => x is AdvancedItemDragBehavior))
        {
            return;
        }

        behaviors.Add(this);
    }

    private static bool SupportsContainerContent(Control control)
    {
        return control is ContentControl or ContentPresenter;
    }

    private static bool IsContainerContentProperty(Control control, AvaloniaProperty property)
    {
        return control switch
        {
            ContentControl => property == ContentControl.ContentProperty,
            ContentPresenter => property == ContentPresenter.ContentProperty,
            _ => false
        };
    }

    private static object? GetContainerContent(Control control)
    {
        return control switch
        {
            ContentControl contentControl => contentControl.Content,
            ContentPresenter contentPresenter => contentPresenter.Content,
            _ => null
        };
    }

    private void PointerMoved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (_captured
            && properties.IsLeftButtonPressed)
        {
            if (_itemsControl?.Items is null || _draggedContainer?.RenderTransform is null || !_enableDrag)
            {
                return;
            }

            var orientation = Orientation;
            var position = e.GetPosition(_itemsControl);
            var delta = orientation == Orientation.Horizontal ? position.X - _start.X : position.Y - _start.Y;

            if (!_dragStarted)
            {
                var diff = _start - position;
                var horizontalDragThreshold = HorizontalDragThreshold;
                var verticalDragThreshold = VerticalDragThreshold;

                if (orientation == Orientation.Horizontal)
                {
                    if (Math.Abs(diff.X) > horizontalDragThreshold)
                    {
                        _dragStarted = true;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (Math.Abs(diff.Y) > verticalDragThreshold)
                    {
                        _dragStarted = true;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (orientation == Orientation.Horizontal)
            {
                SetTranslateTransform(_draggedContainer, delta, 0);
            }
            else
            {
                SetTranslateTransform(_draggedContainer, 0, delta);
            }

            _draggedIndex = _itemsControl.IndexFromContainer(_draggedContainer);
            _targetIndex = -1;

            var draggedBounds = _draggedContainer.Bounds;

            var draggedStart = orientation == Orientation.Horizontal ? draggedBounds.X : draggedBounds.Y;

            var draggedDeltaStart = orientation == Orientation.Horizontal
                ? draggedBounds.X + delta
                : draggedBounds.Y + delta;

            var draggedDeltaEnd = orientation == Orientation.Horizontal
                ? draggedBounds.X + delta + draggedBounds.Width
                : draggedBounds.Y + delta + draggedBounds.Height;

            var i = 0;

            foreach (var _ in _itemsControl.Items)
            {
                var targetContainer = _itemsControl.ContainerFromIndex(i);
                if (targetContainer?.RenderTransform is null || ReferenceEquals(targetContainer, _draggedContainer))
                {
                    i++;
                    continue;
                }

                var targetBounds = targetContainer.Bounds;

                var targetStart = orientation == Orientation.Horizontal ? targetBounds.X : targetBounds.Y;

                var targetMid = orientation == Orientation.Horizontal
                    ? targetBounds.X + targetBounds.Width / 2
                    : targetBounds.Y + targetBounds.Height / 2;

                var targetIndex = _itemsControl.IndexFromContainer(targetContainer);

                if (targetStart > draggedStart && draggedDeltaEnd >= targetMid)
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        SetTranslateTransform(targetContainer, -draggedBounds.Width, 0);
                    }
                    else
                    {
                        SetTranslateTransform(targetContainer, 0, -draggedBounds.Height);
                    }

                    _targetIndex = _targetIndex == -1 ? targetIndex :
                        targetIndex > _targetIndex ? targetIndex : _targetIndex;
                }
                else if (targetStart < draggedStart && draggedDeltaStart <= targetMid)
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        SetTranslateTransform(targetContainer, draggedBounds.Width, 0);
                    }
                    else
                    {
                        SetTranslateTransform(targetContainer, 0, draggedBounds.Height);
                    }

                    _targetIndex = _targetIndex == -1 ? targetIndex :
                        targetIndex < _targetIndex ? targetIndex : _targetIndex;
                }
                else
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        SetTranslateTransform(targetContainer, 0, 0);
                    }
                    else
                    {
                        SetTranslateTransform(targetContainer, 0, 0);
                    }
                }

                i++;
            }
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

    private void SetTranslateTransform(Control control, double x, double y)
    {
        var transformBuilder = new TransformOperations.Builder(1);
        transformBuilder.AppendTranslate(x, y);
        control.RenderTransform = transformBuilder.Build();
    }
}
