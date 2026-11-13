using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Platforms.Abstraction;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 为滚动视图提供触控越界拉伸和边界触觉反馈。
/// </summary>
public class ScrollViewerAssist
{
    /// <summary>
    /// 是否启用触控越界拉伸和边界触觉反馈。
    /// </summary>
    public static readonly AttachedProperty<bool> IsOverScrollAnimationEnabledProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewerAssist, ScrollViewer, bool>("IsOverScrollAnimationEnabled", true);

    private static readonly AttachedProperty<OverScrollState?> StateProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewerAssist, ScrollViewer, OverScrollState?>("State");

    private static readonly ConditionalWeakTable<TopLevel, GestureCoordinator> Coordinators = new();
    private static readonly Easing ReturnEasing = Easing.Parse("0.2, 0, 0, 1");

    /// <summary>
    /// 设置是否启用触控越界拉伸和边界触觉反馈。
    /// </summary>
    public static void SetIsOverScrollAnimationEnabled(ScrollViewer obj, bool value) =>
        obj.SetValue(IsOverScrollAnimationEnabledProperty, value);

    /// <summary>
    /// 获取是否启用触控越界拉伸和边界触觉反馈。
    /// </summary>
    public static bool GetIsOverScrollAnimationEnabled(ScrollViewer obj) =>
        obj.GetValue(IsOverScrollAnimationEnabledProperty);

    static ScrollViewerAssist()
    {
        Control.LoadedEvent.AddClassHandler<ScrollViewer>((viewer, _) => UpdateState(viewer));
        IsOverScrollAnimationEnabledProperty.Changed.AddClassHandler<ScrollViewer>((viewer, _) => UpdateState(viewer));
    }

    internal static void CancelTouch(Control host)
    {
        if (TopLevel.GetTopLevel(host) is { } root && Coordinators.TryGetValue(root, out var coordinator))
            coordinator.EndTouch();
    }

    private static void UpdateState(ScrollViewer viewer)
    {
        if (viewer.IsLoaded && GetIsOverScrollAnimationEnabled(viewer) && TopLevel.GetTopLevel(viewer) is { } root)
        {
            if (viewer.GetValue(StateProperty) == null)
            {
                viewer.SetValue(StateProperty, new OverScrollState(viewer,
                    Coordinators.GetValue(root, topLevel => new GestureCoordinator(topLevel))));
            }
        }
        else
        {
            viewer.GetValue(StateProperty)?.Dispose();
            viewer.ClearValue(StateProperty);
        }
    }

    private sealed class GestureFrame
    {
        public List<ScrollSample> Samples { get; } = [];
        public ScrollSample? Consumer { get; set; }
    }

    private sealed class ScrollSample(OverScrollState state, Vector delta, Vector units)
    {
        public OverScrollState State { get; } = state;
        public Vector Delta { get; } = delta;
        public Vector Units { get; } = units;
        public Vector Before { get; } = state.Presenter!.Offset;
        public Vector After { get; set; } = state.Presenter!.Offset;
    }

    private sealed class GestureCoordinator(TopLevel root)
    {
        private readonly HashSet<OverScrollState> _states = [];
        private readonly ConditionalWeakTable<ScrollGestureEventArgs, GestureFrame> _frames = new();
        private IDisposable? _rawInputSubscription;
        private IPointer? _pointer;
        private Visual? _touchSource;
        private int? _gestureId;
        private bool _inertia;

        public TopLevel Root { get; } = root;
        public bool IsTouchDown => _touchSource != null && !_inertia;

        public void Add(OverScrollState state)
        {
            if (_states.Count == 0)
            {
                Root.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, true);
                Root.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, true);
                Root.AddHandler(InputElement.ScrollGestureEvent, OnScroll, RoutingStrategies.Bubble, true);
                Root.AddHandler(InputElement.ScrollGestureInertiaStartingEvent, OnInertia, RoutingStrategies.Bubble, true);
                Root.AddHandler(InputElement.ScrollGestureEndedEvent, OnEnded, RoutingStrategies.Bubble, true);
                var inputManager = AvaloniaUnsafeAccessorHelpers.GetAvaloniaLocatorService<IInputManager>();
                if (inputManager != null)
                {
                    _rawInputSubscription = AvaloniaUnsafeAccessorHelpers.GetRawInputPreProcess(inputManager)
                        .Subscribe(OnRawInput);
                }
            }
            _states.Add(state);
        }

        public void Remove(OverScrollState state)
        {
            _states.Remove(state);
            if (_states.Count != 0) return;
            Root.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            Root.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            Root.RemoveHandler(InputElement.ScrollGestureEvent, OnScroll);
            Root.RemoveHandler(InputElement.ScrollGestureInertiaStartingEvent, OnInertia);
            Root.RemoveHandler(InputElement.ScrollGestureEndedEvent, OnEnded);
            _rawInputSubscription?.Dispose();
            _rawInputSubscription = null;
            EndTouch();
        }

        private void OnPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Pointer.Type == PointerType.Touch && _pointer != null && !_inertia && _pointer != e.Pointer) return;
            _pointer = e.Pointer.Type == PointerType.Touch ? e.Pointer : null;
            _touchSource = e.Pointer.Type == PointerType.Touch ? e.Source as Visual : null;
            _gestureId = null;
            _inertia = false;
            foreach (var state in _states)
            {
                state.IsTouchOwner = _touchSource != null &&
                    (state.Presenter == _touchSource || state.Presenter?.IsVisualAncestorOf(_touchSource) == true);
                state.ResetEdges();
                if (state.IsTouchOwner) state.StopReturn();
                else state.ReturnToRest();
            }
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Pointer == _pointer && _gestureId == null) EndTouch();
        }

        private void OnRawInput(RawInputEventArgs e)
        {
            if (_pointer == null || e is not RawPointerEventArgs raw) return;
            var type = AvaloniaUnsafeAccessorHelpers.GetRawPointerEventType(raw);
            // Gesture capture can bypass routed release, and cancellation may never start a scroll gesture.
            if ((type == RawPointerEventType.TouchCancel || type == RawPointerEventType.TouchEnd && _gestureId == null) &&
                AvaloniaUnsafeAccessorHelpers.GetRawInputDevice(raw) is IPointerDevice device &&
                AvaloniaUnsafeAccessorHelpers.GetRawInputPointer(device, raw) == _pointer)
            {
                EndTouch();
            }
        }

        public GestureFrame? GetFrame(OverScrollState state, ScrollGestureEventArgs e)
        {
            if (!state.IsTouchOwner || _touchSource == null || _gestureId.HasValue && _gestureId != e.Id)
                return null;
            _gestureId = e.Id;
            return _frames.GetValue(e, _ => new GestureFrame());
        }

        public GestureFrame? FindFrame(ScrollGestureEventArgs e) => _frames.TryGetValue(e, out var frame) ? frame : null;

        public void CancelTouch(OverScrollState state)
        {
            if (state.IsTouchOwner) EndTouch();
        }

        private void OnScroll(object? sender, ScrollGestureEventArgs e)
        {
            if (!_frames.TryGetValue(e, out var frame)) return;
            _frames.Remove(e);
            if (frame.Samples.Count == 0) return;
            // Let ancestors (including drawers) finish routing before claiming an unconsumed gesture.
            var sample = frame.Consumer ?? (!e.Handled ? frame.Samples.LastOrDefault() : null);
            foreach (var state in _states)
            {
                if (state != sample?.State) state.ReturnToRest();
            }
            if (sample == null) return;
            var owner = sample.State;
            var edges = owner.UpdateEdges(sample);
            var consumed = new Vector((sample.After.X - sample.Before.X) * sample.Units.X,
                (sample.After.Y - sample.Before.Y) * sample.Units.Y);
            var remaining = sample.Delta - consumed;
            var overrun = owner.GetOverrun(remaining);
            if (_inertia)
            {
                // An inertial impact gets one pulse, rather than accumulating on every inertia tick.
                if (edges != 0)
                {
                    var impact = owner.GetOverrun(sample.Delta);
                    owner.Pull(new Vector((edges & 3) != 0 ? impact.X : 0, (edges & 12) != 0 ? impact.Y : 0));
                    owner.ReturnToRest();
                }
            }
            else if (overrun != default)
            {
                owner.Pull(overrun);
                e.Handled = true;
                e.ShouldEndScrollGesture = false;
            }
        }

        private void OnInertia(object? sender, ScrollGestureInertiaStartingEventArgs e)
        {
            if (_gestureId != e.Id) return;
            _inertia = true;
            foreach (var state in _states) state.ReturnToRest();
        }

        private void OnEnded(object? sender, ScrollGestureEndedEventArgs e)
        {
            if (_gestureId == e.Id) EndTouch();
        }

        public void EndTouch()
        {
            _pointer = null;
            _touchSource = null;
            _gestureId = null;
            _inertia = false;
            foreach (var state in _states)
            {
                state.IsTouchOwner = false;
                state.ReturnToRest();
            }
        }
    }

    private sealed class OverScrollState : IDisposable
    {
        private readonly ScrollViewer _viewer;
        private readonly GestureCoordinator _coordinator;
        private IDisposable? _gestureSubscription;
        private BoundaryScrollRecognizer? _recognizer;
        private CompositionVisual? _visual;
        private Vector3D _originalScale;
        private Vector3D _originalCenter;
        private Vector _distance;
        private int _edges;
        private int _returnVersion;
        private bool _returning;
        private bool _forwarding;

        public ScrollContentPresenter? Presenter { get; private set; }
        public bool IsTouchOwner { get; set; }
        private bool CanAnimate => IThemeService.AnimationLevel > 0 && !IThemeService.IsTransientDisabled;

        public OverScrollState(ScrollViewer viewer, GestureCoordinator coordinator)
        {
            _viewer = viewer;
            _coordinator = coordinator;
            viewer.TemplateApplied += OnTemplateApplied;
            viewer.PropertyChanged += OnViewerChanged;
            viewer.DetachedFromVisualTree += OnDetached;
            coordinator.Add(this);
            AttachPresenter();
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) => AttachPresenter();

        private void OnViewerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ScrollViewer.IsScrollInertiaEnabledProperty) UpdateRecognizer();
        }

        private void AttachPresenter()
        {
            DetachPresenter();
            Presenter = _viewer.Presenter as ScrollContentPresenter;
            if (Presenter == null) return;
            ReplaceRecognizer();
            // Register after loading: ancestor controls' class handlers must get first refusal.
            _gestureSubscription = InputElement.ScrollGestureEvent.AddClassHandler<ScrollContentPresenter>(BeforeScroll,
                handledEventsToo: true);
            Presenter.AddHandler(InputElement.ScrollGestureEvent, AfterScroll, RoutingStrategies.Bubble, true);
            Presenter.PropertyChanged += OnPresenterChanged;
        }

        private void DetachPresenter()
        {
            _coordinator.CancelTouch(this);
            IsTouchOwner = false;
            StopReturn();
            RestoreVisual();
            _distance = default;
            _gestureSubscription?.Dispose();
            _gestureSubscription = null;
            if (Presenter != null)
            {
                Presenter.RemoveHandler(InputElement.ScrollGestureEvent, AfterScroll);
                Presenter.PropertyChanged -= OnPresenterChanged;
                if (_recognizer != null)
                {
                    _recognizer.Cancel();
                    Presenter.GestureRecognizers.Remove(_recognizer);
                }
            }
            _recognizer = null;
            Presenter = null;
        }

        private void OnPresenterChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ContentPresenter.ChildProperty || e.Property == ScrollContentPresenter.ExtentProperty ||
                e.Property == ScrollContentPresenter.ViewportProperty || e.Property == Visual.BoundsProperty ||
                e.Property == ScrollContentPresenter.CanHorizontallyScrollProperty ||
                e.Property == ScrollContentPresenter.CanVerticallyScrollProperty)
            {
                StopReturn();
                _distance = default;
                RestoreVisual();
                if (e.Property == ContentPresenter.ChildProperty)
                {
                    _coordinator.CancelTouch(this);
                    IsTouchOwner = false;
                    ReplaceRecognizer();
                }
                else UpdateRecognizer();
            }
        }

        private void ReplaceRecognizer()
        {
            if (Presenter == null) return;
            if (_recognizer != null)
            {
                _recognizer.Cancel();
                Presenter.GestureRecognizers.Remove(_recognizer);
            }
            _recognizer = new BoundaryScrollRecognizer();
            Presenter.GestureRecognizers.Add(_recognizer);
            UpdateRecognizer();
        }

        private void UpdateRecognizer()
        {
            if (_recognizer == null) return;
            _recognizer.CanHorizontallyScroll = CanScrollX;
            _recognizer.CanVerticallyScroll = CanScrollY;
            _recognizer.IsScrollInertiaEnabled = _viewer.IsScrollInertiaEnabled;
        }

        private bool CanScrollX => Presenter is { CanHorizontallyScroll: true } p && p.Extent.Width > p.Viewport.Width && p.Bounds.Width > 0;
        private bool CanScrollY => Presenter is { CanVerticallyScroll: true } p && p.Extent.Height > p.Viewport.Height && p.Bounds.Height > 0;

        private Vector AdjustDirection(Vector delta) => _viewer.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft
            ? new Vector(-delta.X, delta.Y) : delta;

        private Vector GetUnits()
        {
            if (Presenter?.Child is ILogicalScrollable { IsLogicalScrollEnabled: true } scrollable)
            {
                return new Vector(scrollable.Viewport.Width > 0 ? Presenter.Bounds.Width / scrollable.Viewport.Width : 1,
                    scrollable.Viewport.Height > 0 ? Presenter.Bounds.Height / scrollable.Viewport.Height : 1);
            }
            return new Vector(1, 1);
        }

        private void BeforeScroll(ScrollContentPresenter presenter, ScrollGestureEventArgs e)
        {
            if (presenter != Presenter || e.Handled || !IsTouchOwner ||
                !(CanScrollX && e.Delta.X != 0 || CanScrollY && e.Delta.Y != 0)) return;
            var frame = _coordinator.GetFrame(this, e);
            if (frame == null) return;
            var delta = AdjustDirection(e.Delta);
            if (!_forwarding && _coordinator.IsTouchDown && _distance != default)
            {
                var rest = new Vector(ConsumePull(delta.X, true), ConsumePull(delta.Y, false));
                if (rest != delta)
                {
                    ApplyStretch();
                    e.Handled = true;
                    if (rest != default && e.Source is InputElement source)
                    {
                        _forwarding = true;
                        try
                        {
                            var forwarded = new ScrollGestureEventArgs(e.Id, AdjustDirection(rest));
                            source.RaiseEvent(forwarded);
                            e.ShouldEndScrollGesture = forwarded.ShouldEndScrollGesture;
                        }
                        finally
                        {
                            _forwarding = false;
                        }
                    }
                    return;
                }
            }
            frame.Samples.Add(new ScrollSample(this, delta, GetUnits()));
        }

        private double ConsumePull(double value, bool horizontal)
        {
            var distance = horizontal ? _distance.X : _distance.Y;
            if (value * distance >= 0) return value;
            var consumed = Math.CopySign(Math.Min(Math.Abs(value), Math.Abs(distance)), value);
            _distance = horizontal ? _distance.WithX(distance + consumed) : _distance.WithY(distance + consumed);
            return value - consumed;
        }

        private void AfterScroll(object? sender, ScrollGestureEventArgs e)
        {
            var frame = _coordinator.FindFrame(e);
            var sample = frame?.Samples.LastOrDefault();
            if (sample?.State != this || Presenter == null) return;
            sample.After = Presenter.Offset;
            if (e.Handled) frame!.Consumer = sample;
        }

        public Vector GetOverrun(Vector delta)
        {
            if (Presenter == null) return default;
            var units = GetUnits();
            return new Vector(CanScrollX && AtEdge(Presenter.Offset.X, Presenter.Extent.Width - Presenter.Viewport.Width, delta.X, units.X) ? delta.X : 0,
                CanScrollY && AtEdge(Presenter.Offset.Y, Presenter.Extent.Height - Presenter.Viewport.Height, delta.Y, units.Y) ? delta.Y : 0);
        }

        private static bool AtEdge(double offset, double maximum, double delta, double units) =>
            delta < 0 && offset * units <= 0.1 || delta > 0 && (maximum - offset) * units <= 0.1;

        public void ResetEdges() => _edges = 0;

        public int UpdateEdges(ScrollSample sample)
        {
            if (Presenter == null) return 0;
            var units = GetUnits();
            var maximum = Presenter.Extent - Presenter.Viewport;
            var touching = (CanScrollX && Presenter.Offset.X * units.X <= 0.1 ? 1 : 0) |
                           (CanScrollX && (maximum.Width - Presenter.Offset.X) * units.X <= 0.1 ? 2 : 0) |
                           (CanScrollY && Presenter.Offset.Y * units.Y <= 0.1 ? 4 : 0) |
                           (CanScrollY && (maximum.Height - Presenter.Offset.Y) * units.Y <= 0.1 ? 8 : 0);
            _edges &= touching;
            var outward = (sample.Delta.X < 0 ? 1 : sample.Delta.X > 0 ? 2 : 0) |
                          (sample.Delta.Y < 0 ? 4 : sample.Delta.Y > 0 ? 8 : 0);
            var arrived = touching & outward & ~_edges;
            _edges |= touching & outward;
            var approached = (sample.Before.X * units.X > 0.1 ? 1 : 0) |
                             ((maximum.Width - sample.Before.X) * units.X > 0.1 ? 2 : 0) |
                             (sample.Before.Y * units.Y > 0.1 ? 4 : 0) |
                             ((maximum.Height - sample.Before.Y) * units.Y > 0.1 ? 8 : 0);
            if ((arrived & approached) != 0) PlatformServices.HapticFeedbackService.PlayScrollBoundary();
            return arrived;
        }

        public void Pull(Vector delta)
        {
            StopReturn();
            if (!CanAnimate)
            {
                _distance = default;
                RestoreVisual();
                return;
            }
            _distance += delta;
            ApplyStretch();
        }

        public void StopReturn()
        {
            ++_returnVersion;
            _returning = false;
        }

        public void ReturnToRest()
        {
            if (_returning || _distance == default) return;
            if (!CanAnimate)
            {
                StopReturn();
                _distance = default;
                RestoreVisual();
                return;
            }
            _returning = true;
            var version = ++_returnVersion;
            var start = Stopwatch.GetTimestamp();
            var distance = _distance;
            _coordinator.Root.RequestAnimationFrame(Animate);

            void Animate(TimeSpan timestamp)
            {
                if (version != _returnVersion) return;
                var progress = CanAnimate ? Math.Min(1, Stopwatch.GetElapsedTime(start).TotalMilliseconds / 300) : 1;
                _distance = distance * (1 - ReturnEasing.Ease(progress));
                ApplyStretch();
                if (progress < 1) _coordinator.Root.RequestAnimationFrame(Animate);
                else
                {
                    _returning = false;
                    RestoreVisual();
                }
            }
        }

        private void ApplyStretch()
        {
            if (Presenter?.Child is not { } child || _distance == default)
            {
                RestoreVisual();
                return;
            }
            var visual = ElementComposition.GetElementVisual(child);
            if (visual == null) return;
            if (_visual != visual)
            {
                RestoreVisual();
                _visual = visual;
                _originalScale = visual.Scale;
                _originalCenter = visual.CenterPoint;
            }
            var anchor = Presenter.TranslatePoint(new Point(_distance.X > 0 ? Presenter.Bounds.Width : 0,
                _distance.Y > 0 ? Presenter.Bounds.Height : 0), child);
            if (anchor == null) return;
            visual.CenterPoint = new Vector3D(anchor.Value.X, anchor.Value.Y, _originalCenter.Z);
            visual.Scale = new Vector3D(_originalScale.X * Stretch(_distance.X, Presenter.Bounds.Width),
                _originalScale.Y * Stretch(_distance.Y, Presenter.Bounds.Height), _originalScale.Z);
        }

        private static double Stretch(double distance, double viewport) =>
            1 + 0.08 * (Math.Abs(distance) / (0.25 * viewport + Math.Abs(distance)));

        private void RestoreVisual()
        {
            if (_visual == null) return;
            _visual.Scale = _originalScale;
            _visual.CenterPoint = _originalCenter;
            _visual = null;
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Dispose();
            _viewer.ClearValue(StateProperty);
        }

        public void Dispose()
        {
            _viewer.TemplateApplied -= OnTemplateApplied;
            _viewer.PropertyChanged -= OnViewerChanged;
            _viewer.DetachedFromVisualTree -= OnDetached;
            DetachPresenter();
            _coordinator.Remove(this);
        }
    }

    private sealed class BoundaryScrollRecognizer : ScrollGestureRecognizer
    {
        private PointerPressedEventArgs? _press;
        private bool _cancelled;

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (_cancelled || e.Pointer.Type != PointerType.Touch) return;
            _press = e;
            base.PointerPressed(e);
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (!_cancelled && !e.Handled) base.PointerMoved(e);
        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if (!_cancelled) base.PointerReleased(e);
        }

        public void Cancel()
        {
            _cancelled = true;
            if (_press == null) return;
            // Restarting ends any running inertia before Target is cleared by collection removal.
            base.PointerPressed(_press);
            base.PointerCaptureLost(_press.Pointer);
            _press = null;
        }
    }
}
