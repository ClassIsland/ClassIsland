// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//
// Modified by HelloWRC, 2025

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using Avalonia.Xaml.Interactivity;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// Behavior that initiates an in-process managed drag with an optional preview window.
/// This avoids OS drag-drop and integrates with <see cref="ManagedDragDropService"/>.
/// </summary>
public class AdvancedManagedContextDragBehavior : StyledElementBehavior<Control>
{
    private static bool s_isDragging;

    private Point _dragStartPoint;
    private PointerEventArgs? _triggerEvent;
    private bool _lock;
    private bool _captured;

    private TopLevel? _topLevel;
    private bool _internalDragging;
    private TaskCompletionSource<bool>? _internalDragTcs;

    // Stores the calculated preview offset (TopLeftOfControl - PointerPosition) in TopLevel client coordinates when enabled
    private Point? _calculatedPreviewOffset;

    private bool _isTouch;

    /// <summary>
    /// Gets or sets the context value used as a drag payload when the drag starts.
    /// </summary>
    public static readonly StyledProperty<object?> ContextProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, object?>(nameof(Context));

    /// <summary>
    /// Gets or sets the data template used to render the drag preview window content.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> PreviewTemplateProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, IDataTemplate?>(nameof(PreviewTemplate));

    /// <summary>
    /// Gets or sets the minimal horizontal distance required to start dragging.
    /// </summary>
    public static readonly StyledProperty<double> HorizontalDragThresholdProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, double>(nameof(HorizontalDragThreshold), 3);

    /// <summary>
    /// Gets or sets the minimal vertical distance required to start dragging.
    /// </summary>
    public static readonly StyledProperty<double> VerticalDragThresholdProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, double>(nameof(VerticalDragThreshold), 3);

    /// <summary>
    /// Gets or sets the fixed logical offset applied to the preview position.
    /// Ignored when <see cref="UsePointerRelativePreviewOffset"/> is true and a pointer-relative offset is calculated.
    /// </summary>
    public static readonly StyledProperty<Point> PreviewOffsetProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, Point>(nameof(PreviewOffset), new Point(0, 0));

    /// <summary>
    /// Gets or sets the data format name used to identify the payload in managed drag operations.
    /// </summary>
    public static readonly StyledProperty<string> DataFormatProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, string>(nameof(DataFormat), "Context");

    /// <summary>
    /// Gets or sets whether to compute a pointer-relative preview offset automatically.
    /// </summary>
    public static readonly StyledProperty<bool> UsePointerRelativePreviewOffsetProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, bool>(nameof(UsePointerRelativePreviewOffset), true);

    /// <summary>
    /// Gets or sets the preview window opacity.
    /// </summary>
    public static readonly StyledProperty<double> PreviewOpacityProperty =
        AvaloniaProperty.Register<ManagedContextDragBehavior, double>(nameof(PreviewOpacity), 0.65);

    /// <summary>
    /// Gets or sets the context value used as a drag payload when the drag starts.
    /// </summary>
    public object? Context
    {
        get => GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    /// <summary>
    /// Gets or sets the template used to render the drag preview.
    /// </summary>
    public IDataTemplate? PreviewTemplate
    {
        get => GetValue(PreviewTemplateProperty);
        set => SetValue(PreviewTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimal horizontal distance required to start dragging.
    /// </summary>
    public double HorizontalDragThreshold
    {
        get => GetValue(HorizontalDragThresholdProperty);
        set => SetValue(HorizontalDragThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimal vertical distance required to start dragging.
    /// </summary>
    public double VerticalDragThreshold
    {
        get => GetValue(VerticalDragThresholdProperty);
        set => SetValue(VerticalDragThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets a fixed logical offset added to the preview position.
    /// </summary>
    public Point PreviewOffset
    {
        get => GetValue(PreviewOffsetProperty);
        set => SetValue(PreviewOffsetProperty, value);
    }

    /// <summary>
    /// Gets or sets the data format name used to identify the managed payload.
    /// </summary>
    public string DataFormat
    {
        get => GetValue(DataFormatProperty);
        set => SetValue(DataFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to compute a pointer-relative preview offset automatically.
    /// </summary>
    public bool UsePointerRelativePreviewOffset
    {
        get => GetValue(UsePointerRelativePreviewOffsetProperty);
        set => SetValue(UsePointerRelativePreviewOffsetProperty, value);
    }

    /// <summary>
    /// Gets or sets the preview window opacity.
    /// </summary>
    public double PreviewOpacity
    {
        get => GetValue(PreviewOpacityProperty);
        set => SetValue(PreviewOpacityProperty, value);
    }

    public static readonly StyledProperty<bool> AllowDragFromDragThumbOnlyProperty = AvaloniaProperty.Register<AdvancedManagedContextDragBehavior, bool>(
        nameof(AllowDragFromDragThumbOnly));

    public bool AllowDragFromDragThumbOnly
    {
        get => GetValue(AllowDragFromDragThumbOnlyProperty);
        set => SetValue(AllowDragFromDragThumbOnlyProperty, value);
    }

    public static readonly StyledProperty<bool> CanDragWithoutDragThumbProperty = AvaloniaProperty.Register<AdvancedManagedContextDragBehavior, bool>(
        nameof(CanDragWithoutDragThumb));

    public bool CanDragWithoutDragThumb
    {
        get => GetValue(CanDragWithoutDragThumbProperty);
        set => SetValue(CanDragWithoutDragThumbProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree()
    {
        var ao = AssociatedObject;
        _topLevel = ao != null ? TopLevel.GetTopLevel(ao) : null;
        ao?.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ao?.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ao?.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ao?.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree()
    {
        var ao = AssociatedObject;
        ao?.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        ao?.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        ao?.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        ao?.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
        DetachTopLevelHandlers();
        _topLevel = null;
    }

    private void AttachTopLevelHandlers()
    {
        if (_topLevel is null) return;
        _topLevel.AddHandler(InputElement.PointerMovedEvent, OnTopLevelPointerMoved, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _topLevel.AddHandler(InputElement.PointerReleasedEvent, OnTopLevelPointerReleased, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _topLevel.AddHandler(InputElement.PointerCaptureLostEvent, OnTopLevelPointerCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void DetachTopLevelHandlers()
    {
        if (_topLevel is null) return;
        _topLevel.RemoveHandler(InputElement.PointerMovedEvent, OnTopLevelPointerMoved);
        _topLevel.RemoveHandler(InputElement.PointerReleasedEvent, OnTopLevelPointerReleased);
        _topLevel.RemoveHandler(InputElement.PointerCaptureLostEvent, OnTopLevelPointerCaptureLost);
    }

    private static DragDropEffects GetDesiredEffects(PointerEventArgs triggerEvent)
    {
        var effect = DragDropEffects.Move;
        if (triggerEvent.KeyModifiers.HasFlag(KeyModifiers.Alt)) effect = DragDropEffects.Link;
        else if (triggerEvent.KeyModifiers.HasFlag(KeyModifiers.Control)) effect = DragDropEffects.Copy;
        return effect;
    }

    private static string EffectsToStatus(DragDropEffects effects)
    {
        return effects switch
        {
            DragDropEffects.Move => "Move",
            DragDropEffects.Copy => "Copy",
            DragDropEffects.Link => "Link",
            DragDropEffects.None => string.Empty,
            _ => effects.ToString()
        };
    }

    private async Task StartInternalDragAsync(PointerEventArgs triggerEvent, object value)
    {
        var effects = GetDesiredEffects(triggerEvent);
        var tl = _topLevel ?? TopLevel.GetTopLevel(AssociatedObject);
        if (tl is null) return;

        var client = triggerEvent.GetPosition(tl);
        var previewOffset = UsePointerRelativePreviewOffset && _calculatedPreviewOffset.HasValue
            ? _calculatedPreviewOffset.Value
            : PreviewOffset;

        DragPreviewService.Show(value, PreviewTemplate, tl, client, previewOffset, PreviewOpacity);

        try
        {
            s_isDragging = true;
            _internalDragging = true;
            _internalDragTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            try { if (AssociatedObject != null) triggerEvent.Pointer?.Capture(AssociatedObject); } catch { }
            AttachTopLevelHandlers();
            ManagedDragDropService.Instance.Begin(tl, value, DataFormat, effects, client);
            if (AssociatedObject != null)
                AssociatedObject.DetachedFromVisualTree += AssociatedObject_DetachedFromVisualTree;
            await _internalDragTcs.Task.ConfigureAwait(true);
        }
        finally
        {
            ManagedDragDropService.Instance.End();
            if (AssociatedObject != null)
                AssociatedObject.DetachedFromVisualTree -= AssociatedObject_DetachedFromVisualTree;
            DetachTopLevelHandlers();
            try { triggerEvent.Pointer?.Capture(null); } catch { }
            DragPreviewService.Hide();
            _internalDragging = false;
            _internalDragTcs = null;
            s_isDragging = false;
            _calculatedPreviewOffset = null;
        }
    }

    private void AssociatedObject_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _internalDragging = false;
        _internalDragTcs?.TrySetResult(true);
    }

    private void OnTopLevelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_internalDragging || _topLevel is null) return;
        var client = e.GetPosition(_topLevel);
        var previewOffset = UsePointerRelativePreviewOffset && _calculatedPreviewOffset.HasValue
            ? _calculatedPreviewOffset.Value
            : PreviewOffset;
        DragPreviewService.Move(_topLevel, client, previewOffset);
        ManagedDragDropService.Instance.Move(client);
    }

    private void OnTopLevelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_internalDragging) return;
        _internalDragging = false;
        _internalDragTcs?.TrySetResult(true);
    }

    private void OnTopLevelPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isTouch)
        {
            return;
        }
        if (!_internalDragging) return;
        _internalDragging = false;
        _internalDragTcs?.TrySetResult(true);
    }

    private void Release()
    {
        _triggerEvent = null;
        _lock = false;
        _calculatedPreviewOffset = null;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var ao = AssociatedObject;
        if (ao is null) return;
        _isTouch = e.Pointer.Type == PointerType.Touch;
        var isFromDragThumb = (e.Source as Control)?.FindAncestorOfType<TouchDragThumb>() is not null;
        var isFromCurrentThumb = e.Source is Control c1 && UITreeHelper.HasParent(c1, AssociatedObject);
        if ((e.Source as Control)?.FindAncestorOfType<ISelectable>() is {} selectable && !Equals(selectable, AssociatedObject))
        {
            isFromCurrentThumb = false;
        }
        var isTouchMode = e.Pointer.Type == PointerType.Touch;
        // AssociatedObject?.ShowToast($"(debug) {isFromDragThumb} {isFromCurrentThumb} {isTouchMode}");
        if ((((!CanDragWithoutDragThumb && isTouchMode) || AllowDragFromDragThumbOnly) && !isFromDragThumb) 
            || (isFromDragThumb && !isFromCurrentThumb))
        {
            return;
        }
        var properties = e.GetCurrentPoint(ao).Properties;
        if (properties.IsLeftButtonPressed && IsEnabled)
        {
            if (e.Source is Control control && ao.DataContext == control.DataContext)
            {
                if ((control as ISelectable ?? control.Parent as ISelectable ?? control.FindLogicalAncestorOfType<ISelectable>())?.IsSelected ?? false)
                    e.Handled = true;

                _dragStartPoint = e.GetPosition(null);
                _triggerEvent = e;
                _lock = true;
                _captured = true;

                // Compute the cursor-relative preview offset if enabled in TopLevel coordinates using AssociatedObject top-left
                if (UsePointerRelativePreviewOffset)
                {
                    _calculatedPreviewOffset = -e.GetPosition(ao);
                }
            }
        }
        e.Handled = false;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_captured)
        {
            if (e.InitialPressMouseButton == MouseButton.Left && _triggerEvent is not null)
            {
                Release();
            }
            _captured = false;
        }
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var ao = AssociatedObject;
        if (ao is null) return;
        if (!_captured || _triggerEvent is null || !IsEnabled)
            return;

        if (s_isDragging)
            return;

        var properties = e.GetCurrentPoint(ao).Properties;
        if (!properties.IsLeftButtonPressed)
            return;

        var point = e.GetPosition(null);
        var diff = _dragStartPoint - point;
        if (Math.Abs(diff.X) > HorizontalDragThreshold || Math.Abs(diff.Y) > VerticalDragThreshold)
        {
            if (_lock) _lock = false; else return;

            var context = Context ?? AssociatedObject?.DataContext;
            if (context is null)
                return;

            await StartInternalDragAsync(_triggerEvent, context);
            _triggerEvent = null;
        }
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isTouch)
        {
            return;
        }
        Release();
        _captured = false;
        if (_internalDragging)
        {
            _internalDragging = false;
            _internalDragTcs?.TrySetResult(true);
        }
    }
}
