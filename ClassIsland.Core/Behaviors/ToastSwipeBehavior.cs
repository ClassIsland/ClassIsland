using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Behaviors;

/// <summary>
/// 为窗口 Toast 提供双向滑动关闭和受限回弹。
/// </summary>
public class ToastSwipeBehavior : StyledElementBehavior<Border>
{
    private static readonly TimeSpan ReturnDuration = TimeSpan.FromMilliseconds(300);
    private static readonly Easing ReturnEasing = Easing.Parse("0.2, 0, 0, 1");
    private readonly TranslateTransform _translation = new();
    private readonly RotateTransform _rotation = new();
    private readonly Queue<(long Time, double X)> _samples = new();
    private AppToastAdorner? _adorner;
    private TopLevel? _topLevel;
    private Control? _root;
    private Control? _item;
    private ToastMessage? _message;
    private IPointer? _pointer;
    private Point _pressPosition;
    private double _startOffset;
    private int _originalZIndex;
    private bool _dragging;
    private bool _interacting;
    private bool _closing;
    private int _animationVersion;
    private IDisposable? _returnTimer;
    private CancellationTokenRegistration _closedRegistration;

    private bool CanAnimate => IThemeService.AnimationLevel > 0 && !IThemeService.IsTransientDisabled;
    private double Threshold => Math.Clamp((AssociatedObject?.Bounds.Width ?? 0) * 0.25, 48, 96);

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject == null) return;
        _topLevel = TopLevel.GetTopLevel(AssociatedObject);
        _adorner = AssociatedObject.FindAncestorOfType<AppToastAdorner>();
        _root = AssociatedObject.GetVisualParent() as Control;
        _item = AssociatedObject.GetVisualAncestors().OfType<Control>()
            .FirstOrDefault(x => x.GetVisualParent() is StackPanel);
        AssociatedObject.RenderTransform = new TransformGroup { Children = { _rotation, _translation } };
        AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        AssociatedObject.PointerCaptureLost += OnCaptureLost;
        AssociatedObject.DataContextChanged += OnDataContextChanged;
        AttachMessage();
        if (!CanAnimate && _root != null) _root.Transitions = null;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            AssociatedObject.PointerCaptureLost -= OnCaptureLost;
            AssociatedObject.DataContextChanged -= OnDataContextChanged;
        }
        Reset();
        _adorner = null;
        _topLevel = null;
        _root = null;
        _item = null;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Reset();
        AttachMessage();
    }

    private void AttachMessage()
    {
        _message = AssociatedObject?.DataContext as ToastMessage;
        if (_message == null) return;
        var message = _message;
        _closedRegistration = message.ClosedCancellationTokenSource.Token.Register(() =>
            Dispatcher.UIThread.Post(() =>
            {
                if (_message != message || _closing) return;
                StopAnimation();
                ReleasePointer();
                SetInteracting(false);
            }));
    }

    private void Reset()
    {
        _closedRegistration.Dispose();
        StopAnimation();
        ReleasePointer();
        SetInteracting(false);
        _translation.X = _translation.Y = _rotation.Angle = 0;
        _root?.Classes.Remove("swipe-closing");
        if (AssociatedObject != null)
        {
            AssociatedObject.Opacity = 1;
            AssociatedObject.IsHitTestVisible = true;
        }
        _closing = false;
        _message = null;
    }

    private bool IsInteractiveSource(object? source)
    {
        for (var visual = source as Visual; visual != null && visual != AssociatedObject;
             visual = visual.GetVisualParent())
        {
            if (visual == _message?.ActionContent ||
                visual is Button or TextBox or SelectingItemsControl or RangeBase or Thumb or SelectableTextBlock ||
                visual is InputElement { Focusable: true } and not FAInfoBar)
            {
                return true;
            }
        }
        return false;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_pointer != null || _closing || _message is not { IsOpen: true } || _topLevel == null ||
            !e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed || IsInteractiveSource(e.Source))
        {
            return;
        }
        _pointer = e.Pointer;
        _pressPosition = e.GetPosition(_topLevel);
        _samples.Clear();
        Sample(_pressPosition.X);
        // Observe before capture so vertical gestures and ordinary clicks remain available.
        _topLevel.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        _topLevel.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        _topLevel.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer != _pointer || _message is not { IsOpen: true }) return;
        var position = e.GetPosition(_topLevel);
        var delta = position - _pressPosition;
        Sample(position.X);
        if (!_dragging)
        {
            if (Math.Abs(delta.Y) > 8 && Math.Abs(delta.Y) >= Math.Abs(delta.X))
            {
                ReleasePointer();
                return;
            }
            if (Math.Abs(delta.X) <= 8 || Math.Abs(delta.X) <= Math.Abs(delta.Y)) return;
            StopAnimation();
            _startOffset = _translation.X;
            if (!_message.CanUserClose)
            {
                // Invert the resistance when re-grabbing a toast midway through its return.
                _startOffset = Math.Sign(_startOffset) * -Threshold *
                    Math.Log(Math.Max(0.001, 1 - Math.Abs(_startOffset) / Threshold));
            }
            _dragging = true;
            SetInteracting(true);
            e.PreventGestureRecognition();
            e.Pointer.Capture(AssociatedObject);
        }
        MoveTo(delta.X);
        e.Handled = true;
    }

    private void MoveTo(double delta)
    {
        var offset = _startOffset + delta;
        if (_message is { CanUserClose: false })
        {
            offset = Math.Sign(offset) * Math.Min(Threshold - 0.01,
                Threshold * (1 - Math.Exp(-Math.Abs(offset) / Threshold)));
        }
        _translation.X = offset;
        _rotation.Angle = IThemeService.AnimationLevel >= 2 && CanAnimate
            ? Math.Clamp(offset * 0.08, -12, 0)
            : 0;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _pointer || e.InitialPressMouseButton != MouseButton.Left) return;
        var dragging = _dragging;
        var position = e.GetPosition(_topLevel);
        var velocity = Sample(position.X);
        if (dragging) MoveTo(position.X - _pressPosition.X);
        ReleasePointer();
        if (!dragging || _message is not { IsOpen: true }) return;
        e.Handled = true;
        var offset = _translation.X;
        var fling = Math.Abs(offset) >= 16 && Math.Abs(velocity) >= 800 && Math.Sign(offset) == Math.Sign(velocity);
        if (_message.CanUserClose && (Math.Abs(offset) >= Threshold || fling))
        {
            Dismiss(velocity);
        }
        else
        {
            ReturnToRest();
        }
    }

    private double Sample(double x)
    {
        var now = Stopwatch.GetTimestamp();
        _samples.Enqueue((now, x));
        while (_samples.Count > 1 && Stopwatch.GetElapsedTime(_samples.Peek().Time, now).TotalMilliseconds > 100)
        {
            _samples.Dequeue();
        }
        var first = _samples.Peek();
        var seconds = Stopwatch.GetElapsedTime(first.Time, now).TotalSeconds;
        return seconds > 0 ? (x - first.X) / seconds : 0;
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (e.Pointer != _pointer || e.Pointer.Captured == AssociatedObject) return;
        var dragging = _dragging;
        ReleasePointer();
        if (dragging && _message is { IsOpen: true }) ReturnToRest();
    }

    private void ReleasePointer()
    {
        var pointer = _pointer;
        _pointer = null;
        _dragging = false;
        _topLevel?.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
        _topLevel?.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
        _topLevel?.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
        if (pointer?.Captured == AssociatedObject) pointer?.Capture(null);
    }

    private void SetInteracting(bool value)
    {
        if (_interacting == value) return;
        _interacting = value;
        if (_message != null) _adorner?.SetToastInteracting(_message, value);
        if (_item == null) return;
        if (value)
        {
            _originalZIndex = _item.ZIndex;
            _item.ZIndex = 1;
        }
        else
        {
            _item.ZIndex = _originalZIndex;
        }
    }

    private void StopAnimation()
    {
        ++_animationVersion;
        _returnTimer?.Dispose();
        _returnTimer = null;
        // Keep the displayed values, not the transition targets, when a drag interrupts the return.
        var x = _translation.X;
        var y = _translation.Y;
        var angle = _rotation.Angle;
        _translation.Transitions = null;
        _rotation.Transitions = null;
        _translation.X = x;
        _translation.Y = y;
        _rotation.Angle = angle;
    }

    private void ReturnToRest()
    {
        StopAnimation();
        if (CanAnimate)
        {
            _translation.Transitions =
            [
                new DoubleTransition { Property = TranslateTransform.XProperty, Duration = ReturnDuration, Easing = ReturnEasing }
            ];
            _rotation.Transitions =
            [
                new DoubleTransition { Property = RotateTransform.AngleProperty, Duration = ReturnDuration, Easing = ReturnEasing }
            ];
            _returnTimer = DispatcherTimer.RunOnce(() =>
            {
                _returnTimer = null;
                SetInteracting(false);
            }, ReturnDuration);
        }
        _translation.X = _translation.Y = _rotation.Angle = 0;
        if (!CanAnimate) SetInteracting(false);
    }

    private void Dismiss(double velocity)
    {
        if (_message is not { IsOpen: true } || AssociatedObject == null || _topLevel == null) return;
        StopAnimation();
        _closing = true;
        _root?.Classes.Add("swipe-closing");
        AssociatedObject.IsHitTestVisible = false;
        _message.Close();
        if (!CanAnimate)
        {
            AssociatedObject.Opacity = 0;
            return;
        }

        var x = _translation.X;
        var angle = _rotation.Angle;
        var right = x > 0;
        var duration = right ? 0.25 : 0.3;
        var fullMotion = IThemeService.AnimationLevel >= 2;
        var speed = Math.Clamp(Math.Abs(Math.Min(velocity, 0)), 800, 2400);
        var rightEdge = AssociatedObject.TranslatePoint(new Point(), _topLevel)?.X ?? 0;
        var distance = Math.Max(AssociatedObject.Bounds.Width, _topLevel.Bounds.Width - rightEdge + 16);
        var started = Stopwatch.GetTimestamp();
        var version = _animationVersion;
        _topLevel.RequestAnimationFrame(Animate);

        void Animate(TimeSpan timestamp)
        {
            if (version != _animationVersion || AssociatedObject == null || _topLevel == null) return;
            var elapsed = Math.Min(duration, Stopwatch.GetElapsedTime(started).TotalSeconds);
            var progress = elapsed / duration;
            if (right)
            {
                _translation.X = x + distance * ReturnEasing.Ease(progress);
                _rotation.Angle = angle * (1 - ReturnEasing.Ease(progress));
            }
            else
            {
                _translation.X = x - speed * elapsed;
                _translation.Y = fullMotion ? 1800 * elapsed * elapsed : 0;
                _rotation.Angle = fullMotion ? angle - 60 * elapsed : 0;
            }
            AssociatedObject.Opacity = 1 - progress * progress;
            if (progress < 1) _topLevel.RequestAnimationFrame(Animate);
        }
    }
}
