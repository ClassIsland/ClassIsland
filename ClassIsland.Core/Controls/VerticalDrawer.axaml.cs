using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;

namespace ClassIsland.Core.Controls;

[TemplatePart("PART_Canvas", typeof(Canvas))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_TitleContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_DismissLayer", typeof(Border))]
[TemplatePart("PART_BackgroundElement", typeof(Border))]
[TemplatePart("PART_TitleBar", typeof(Grid))]
[TemplatePart("WorkingPanel", typeof(Panel))]
[PseudoClasses(":opened", ":closed", ":collapsed", ":can-collapse", ":animated", ":dragging", ":fullscreen", ":layout-ready")]
public class VerticalDrawer : ContentControl
{
    public static FuncValueConverter<double, double> ContainerHeightToClosedCanvasBottomPosConverter { get; } =
        new(x => -x - 2);
    
    public static FuncValueConverter<double, double> ContainerHeightToCollapsedCanvasBottomPosConverter { get; } =
        new(x => -x + 40);

    public static FuncMultiValueConverter<double, double> DrawerWidthConverter { get; } = new(values =>
    {
        var list = values.ToList();
        var availableWidth = list.Count > 0 && double.IsFinite(list[0]) ? Math.Max(0, list[0]) : 0;
        var requestedWidth = list.Count > 1 ? list[1] : double.NaN;
        return double.IsFinite(requestedWidth) && requestedWidth > 0
            ? Math.Min(requestedWidth, availableWidth)
            : double.NaN;
    });
    
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<VerticalDrawer, object?>(
        nameof(Title));

    public static readonly StyledProperty<object?> DrawerContentProperty = AvaloniaProperty.Register<VerticalDrawer, object?>(
        nameof(DrawerContent));

    public object? DrawerContent
    {
        get => GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> DrawerTemplateProperty = AvaloniaProperty.Register<VerticalDrawer, IDataTemplate?>(
        nameof(DrawerTemplate));

    public IDataTemplate? DrawerTemplate
    {
        get => GetValue(DrawerTemplateProperty);
        set => SetValue(DrawerTemplateProperty, value);
    }

    public static readonly StyledProperty<HorizontalAlignment> DrawerHorizontalContentAlignmentProperty = AvaloniaProperty.Register<VerticalDrawer, HorizontalAlignment>(
        nameof(DrawerHorizontalContentAlignment));

    public HorizontalAlignment DrawerHorizontalContentAlignment
    {
        get => GetValue(DrawerHorizontalContentAlignmentProperty);
        set => SetValue(DrawerHorizontalContentAlignmentProperty, value);
    }

    public static readonly StyledProperty<VerticalAlignment> DrawerVerticalContentAlignmentProperty = AvaloniaProperty.Register<VerticalDrawer, VerticalAlignment>(
        nameof(DrawerVerticalContentAlignment));

    public VerticalAlignment DrawerVerticalContentAlignment
    {
        get => GetValue(DrawerVerticalContentAlignmentProperty);
        set => SetValue(DrawerVerticalContentAlignmentProperty, value);
    }

    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty = AvaloniaProperty.Register<VerticalDrawer, IDataTemplate?>(
        nameof(TitleTemplate));

    public IDataTemplate? TitleTemplate
    {
        get => GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    public static readonly StyledProperty<double> ContentHeightRatioProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(ContentHeightRatio), 0.5);

    public double ContentHeightRatio
    {
        get => GetValue(ContentHeightRatioProperty);
        set => SetValue(ContentHeightRatioProperty, value);
    }

    /// <summary>
    /// 抽屉展开后的绝对高度。设置后优先于 <see cref="ContentHeightRatio"/>。
    /// </summary>
    public static readonly StyledProperty<double> ContentHeightProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(ContentHeight), double.NaN);

    /// <inheritdoc cref="ContentHeightProperty"/>
    public double ContentHeight
    {
        get => GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }

    public static readonly StyledProperty<bool> SmokeBackgroundProperty = AvaloniaProperty.Register<VerticalDrawer, bool>(
        nameof(SmokeBackground), true);

    public bool SmokeBackground
    {
        get => GetValue(SmokeBackgroundProperty);
        set => SetValue(SmokeBackgroundProperty, value);
    }

    public static readonly StyledProperty<VerticalDrawerOpenState> StateProperty = AvaloniaProperty.Register<VerticalDrawer, VerticalDrawerOpenState>(
        nameof(State));

    public VerticalDrawerOpenState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>
    /// 是否将抽屉高度展开到整个可用区域，宽度保持不变。撑满高度时 <see cref="State"/> 仍为打开状态。
    /// </summary>
    public static readonly StyledProperty<bool> IsFullScreenProperty = AvaloniaProperty.Register<VerticalDrawer, bool>(
        nameof(IsFullScreen));

    /// <inheritdoc cref="IsFullScreenProperty"/>
    public bool IsFullScreen
    {
        get => GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }

    /// <summary>
    /// 是否允许通过内容区上拉手势将抽屉撑满可用高度，默认禁用。不影响标题栏拖动和直接设置 <see cref="IsFullScreen"/>。
    /// </summary>
    public static readonly StyledProperty<bool> IsFullScreenGestureEnabledProperty =
        AvaloniaProperty.Register<VerticalDrawer, bool>(nameof(IsFullScreenGestureEnabled));

    /// <inheritdoc cref="IsFullScreenGestureEnabledProperty"/>
    public bool IsFullScreenGestureEnabled
    {
        get => GetValue(IsFullScreenGestureEnabledProperty);
        set => SetValue(IsFullScreenGestureEnabledProperty, value);
    }

    /// <summary>
    /// 将标题中的自定义交互控件及其子元素排除在抽屉拖动区域之外。
    /// </summary>
    public static readonly AttachedProperty<bool> IsTitleDragExcludedProperty =
        AvaloniaProperty.RegisterAttached<VerticalDrawer, Control, bool>("IsTitleDragExcluded");

    /// <inheritdoc cref="IsTitleDragExcludedProperty"/>
    public static bool GetIsTitleDragExcluded(Control control) => control.GetValue(IsTitleDragExcludedProperty);

    /// <inheritdoc cref="IsTitleDragExcludedProperty"/>
    public static void SetIsTitleDragExcluded(Control control, bool value) => control.SetValue(IsTitleDragExcludedProperty, value);

    public static readonly StyledProperty<bool> AllowCollapseProperty = AvaloniaProperty.Register<VerticalDrawer, bool>(
        nameof(AllowCollapse));

    public bool AllowCollapse
    {
        get => GetValue(AllowCollapseProperty);
        set => SetValue(AllowCollapseProperty, value);
    }

    public static readonly StyledProperty<double> ContainerLeftPosProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(ContainerLeftPos));

    public double ContainerLeftPos
    {
        get => GetValue(ContainerLeftPosProperty);
        set => SetValue(ContainerLeftPosProperty, value);
    }

    public static readonly StyledProperty<double> ContainerHeightProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(ContainerHeight));

    public double ContainerHeight
    {
        get => GetValue(ContainerHeightProperty);
        set => SetValue(ContainerHeightProperty, value);
    }

    public static readonly StyledProperty<double> RequestedWidthProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(RequestedWidth), double.NaN);

    public double RequestedWidth
    {
        get => GetValue(RequestedWidthProperty);
        set => SetValue(RequestedWidthProperty, value);
    }

    /// <summary>
    /// 抽屉边框的最大宽度，不影响遮罩范围。默认不限制。
    /// </summary>
    public static readonly StyledProperty<double> DrawerMaxWidthProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(DrawerMaxWidth), double.PositiveInfinity, validate: value => value >= 0);

    /// <inheritdoc cref="DrawerMaxWidthProperty"/>
    public double DrawerMaxWidth
    {
        get => GetValue(DrawerMaxWidthProperty);
        set => SetValue(DrawerMaxWidthProperty, value);
    }

    private Border? _dismissLayer;
    private Canvas? _canvas;
    private Panel? _workingPanel;
    private Grid? _titleBar;
    private readonly Queue<(long Time, double Y)> _dragSamples = new();
    private IPointer? _pointer;
    private Point _pointerStart;
    private double _startVisibleHeight;
    private double _dragVisibleHeight;
    private bool _isDragging;
    private Border? _backgroundElement;
    private ScrollGestureRecognizer? _contentRecognizer;
    private IPointer? _contentPointer;
    private InputElement? _contentSource;
    private InputElement? _contentGestureTarget;
    private int? _contentGestureId;
    private Point _contentStart;
    private Point _contentPosition;
    private IDisposable? _contentInputSubscription;
    private bool _contentDragging;
    private bool _contentClosing;
    private bool _contentReleased;
    private bool _contentHandedOff;
    private bool _contentForwarded;
    private bool _forwardingContentGesture;
    private bool _updatingContentState;
    private readonly HashSet<int> _cancelledContentGestures = new();

    static VerticalDrawer()
    {
        // Scroll gestures only bubble. Intercept at their source before a presenter's instance handlers.
        ScrollGestureEvent.AddClassHandler<InputElement>((sender, e) =>
            GetContentGestureOwner(sender, e)?.OnContentScrollGesture(sender, e));
        ScrollGestureInertiaStartingEvent.AddClassHandler<InputElement>((sender, e) =>
        {
            var drawer = GetContentGestureOwner(sender, e);
            if (drawer?._contentGestureId == e.Id && drawer._contentGestureTarget == sender &&
                drawer._contentDragging && !drawer._contentHandedOff)
            {
                e.Handled = true;
                drawer.FinishContentGesture();
            }
        });
        ScrollGestureEndedEvent.AddClassHandler<InputElement>((sender, e) =>
        {
            var drawer = GetContentGestureOwner(sender, e);
            drawer?._cancelledContentGestures.Remove(e.Id);
            if (drawer == null || drawer._contentGestureId != e.Id || drawer._contentGestureTarget != sender)
            {
                return;
            }

            drawer._contentGestureId = null;
            if (drawer._contentForwarded && sender == drawer._backgroundElement && drawer._contentSource is { } source)
            {
                drawer.ForwardContentGestureEnded(source, e.Id);
            }
            if (drawer._contentReleased && drawer._contentDragging && !drawer._contentHandedOff)
            {
                e.Handled = true;
                drawer.FinishContentGesture();
            }
            else
            {
                drawer.CancelContentGesture();
            }
        });
    }

    /// <summary>
    /// 在抽屉即将关闭时触发。可取消关闭。
    /// </summary>
    public event EventHandler<CancelEventArgs>? Closing;

    /// <summary>
    /// 在抽屉关闭后触发。
    /// </summary>
    public event EventHandler? Closed;

    public VerticalDrawer()
    {
        if (IThemeService.AnimationLevel >= 1)
        {
            PseudoClasses.Set(":animated", true);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        PrepareInitialLayout();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelDrag();
        LayoutUpdated -= OnInitialLayoutUpdated;
        PseudoClasses.Set(":layout-ready", false);
        base.OnDetachedFromVisualTree(e);
    }

    private void PrepareInitialLayout()
    {
        PseudoClasses.Set(":layout-ready", false);
        LayoutUpdated -= OnInitialLayoutUpdated;
        LayoutUpdated += OnInitialLayoutUpdated;
    }

    private void OnInitialLayoutUpdated(object? sender, EventArgs e)
    {
        if (_canvas == null || _canvas.Bounds.Height <= 0)
        {
            return;
        }

        // Initial placement must be applied before transitions can interpolate Canvas.Bottom from NaN.
        UpdatePos();
        PseudoClasses.Set(":layout-ready", true);
        LayoutUpdated -= OnInitialLayoutUpdated;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdateContent();
        UpdatePos();
        KeyDown += OnKeyDown;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsFullScreenGestureEnabledProperty)
        {
            CancelContentGesture();
            return;
        }
        if (change.Property != StateProperty && change.Property != IsFullScreenProperty &&
            change.Property != AllowCollapseProperty && change.Property != ContentHeightRatioProperty &&
            change.Property != ContentHeightProperty && change.Property != RequestedWidthProperty && change.Property != DrawerMaxWidthProperty &&
            change.Property != DrawerContentProperty && change.Property != DrawerTemplateProperty)
        {
            return;
        }

        CancelDrag();
        if (change.Property == IsFullScreenProperty && IsFullScreen)
        {
            SetCurrentValue(StateProperty, VerticalDrawerOpenState.Opened);
        }
        else if (State != VerticalDrawerOpenState.Opened)
        {
            SetCurrentValue(IsFullScreenProperty, false);
        }

        UpdateContent();
        UpdatePos();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }
        
        if (e.Key == Key.Escape)
        {
            Dismiss();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        CancelDrag();
        KeyDown -= OnKeyDown;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelDrag();
        PrepareInitialLayout();
        base.OnApplyTemplate(e);
        
        if (_dismissLayer != null)
        {
            _dismissLayer.PointerReleased -= DismissLayerOnPointerReleased;
        }
        if (_canvas != null)
        {
            _canvas.SizeChanged -= CanvasOnSizeChanged;
        }
        if (_titleBar != null)
        {
            _titleBar.PointerPressed -= TitleBarOnPointerPressed;
            _titleBar.PointerMoved -= TitleBarOnPointerMoved;
            _titleBar.PointerReleased -= TitleBarOnPointerReleased;
            _titleBar.PointerCaptureLost -= TitleBarOnPointerCaptureLost;
        }
        if (_backgroundElement != null)
        {
            _backgroundElement.RemoveHandler(PointerPressedEvent, ContentOnPointerPressed);
            _backgroundElement.RemoveHandler(PointerMovedEvent, ContentOnPointerMoved);
            _backgroundElement.RemoveHandler(PointerReleasedEvent, ContentOnPointerReleased);
            _backgroundElement.RemoveHandler(ScrollGestureEvent, ContentOnUnhandledScrollGesture);
            _backgroundElement.PointerCaptureLost -= ContentOnPointerCaptureLost;
            if (_contentRecognizer != null)
            {
                _backgroundElement.GestureRecognizers.Remove(_contentRecognizer);
            }
        }
        
        _dismissLayer = e.NameScope.Find<Border>("PART_DismissLayer");
        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _workingPanel = e.NameScope.Find<Panel>("WorkingPanel");
        _titleBar = e.NameScope.Find<Grid>("PART_TitleBar");
        _backgroundElement = e.NameScope.Find<Border>("PART_BackgroundElement");

        if (_dismissLayer != null)
        {
            _dismissLayer.PointerReleased += DismissLayerOnPointerReleased;
        }
        if (_canvas != null)
        {
            _canvas.SizeChanged += CanvasOnSizeChanged;
        }
        if (_titleBar != null)
        {
            _titleBar.PointerPressed += TitleBarOnPointerPressed;
            _titleBar.PointerMoved += TitleBarOnPointerMoved;
            _titleBar.PointerReleased += TitleBarOnPointerReleased;
            _titleBar.PointerCaptureLost += TitleBarOnPointerCaptureLost;
        }
        if (_backgroundElement != null)
        {
            _backgroundElement.AddHandler(PointerPressedEvent, ContentOnPointerPressed, RoutingStrategies.Tunnel, true);
            _backgroundElement.AddHandler(PointerMovedEvent, ContentOnPointerMoved, RoutingStrategies.Tunnel, true);
            _backgroundElement.AddHandler(PointerReleasedEvent, ContentOnPointerReleased, RoutingStrategies.Tunnel, true);
            _backgroundElement.AddHandler(ScrollGestureEvent, ContentOnUnhandledScrollGesture);
            _backgroundElement.PointerCaptureLost += ContentOnPointerCaptureLost;
            _contentRecognizer = new ContentScrollGestureRecognizer(this);
            _backgroundElement.GestureRecognizers.Add(_contentRecognizer);
        }

        UpdateContent();
        UpdatePos();
    }

    private static VerticalDrawer? GetContentGestureOwner(InputElement sender, RoutedEventArgs e)
    {
        if (sender != e.Source)
        {
            return null;
        }

        var drawer = sender.FindAncestorOfType<VerticalDrawer>();
        return drawer != null && !drawer._forwardingContentGesture &&
               (drawer._contentPointer != null || drawer._cancelledContentGestures.Count > 0) ? drawer : null;
    }

    private void ContentOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_pointer != null || _contentPointer != null || _workingPanel == null || _canvas == null ||
            State != VerticalDrawerOpenState.Opened ||
            e.Pointer.Type is not (PointerType.Touch or PointerType.Pen) ||
            !e.Properties.IsLeftButtonPressed || e.Source is not InputElement source ||
            source.FindAncestorOfType<VerticalDrawer>() != this ||
            source == _titleBar || _titleBar?.IsVisualAncestorOf(source) == true)
        {
            return;
        }

        _contentPointer = e.Pointer;
        _contentSource = source;
        _contentHandedOff = IsFullScreen;
        _contentStart = _contentPosition = e.GetPosition(null);
        _startVisibleHeight = _workingPanel.Bounds.Height + Canvas.GetBottom(_workingPanel);
        _dragSamples.Clear();
        SampleDragPosition(_contentStart.Y);
        // Captured gesture recognizers receive later input directly, bypassing routed pointer events.
        var inputManager = AvaloniaUnsafeAccessorHelpers.GetAvaloniaLocatorService<IInputManager>();
        if (inputManager != null)
        {
            _contentInputSubscription = AvaloniaUnsafeAccessorHelpers.GetRawInputPreProcess(inputManager)
                .Subscribe(OnContentRawInput);
        }
    }

    private void OnContentRawInput(RawInputEventArgs e)
    {
        if (e is not RawPointerEventArgs raw ||
            AvaloniaUnsafeAccessorHelpers.GetRawInputDevice(raw) is not IPointerDevice device ||
            AvaloniaUnsafeAccessorHelpers.GetRawInputPointer(device, raw) != _contentPointer)
        {
            return;
        }

        _contentPosition = AvaloniaUnsafeAccessorHelpers.GetRawPointerPosition(raw);
        var eventType = AvaloniaUnsafeAccessorHelpers.GetRawPointerEventType(raw);
        if (eventType == RawPointerEventType.TouchCancel)
        {
            CancelContentGesture();
        }
        else if (eventType is RawPointerEventType.TouchEnd or RawPointerEventType.LeftButtonUp)
        {
            _contentReleased = true;
        }
    }

    private bool TryStartContentDrag()
    {
        if (_contentDragging || _contentHandedOff)
        {
            return true;
        }
        if (!IsFullScreenGestureEnabled)
        {
            return false;
        }

        var delta = _contentPosition - _contentStart;
        if (Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) < 8)
        {
            return false;
        }
        if (Math.Abs(delta.Y) <= Math.Abs(delta.X))
        {
            CancelContentGesture(releaseCapture: false);
            return false;
        }
        if (delta.Y >= 0)
        {
            return false;
        }

        _contentDragging = _isDragging = true;
        _dragVisibleHeight = _startVisibleHeight;
        PseudoClasses.Set(":dragging", true);
        return true;
    }

    private void ContentOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer != _contentPointer || _contentHandedOff)
        {
            return;
        }

        _contentPosition = e.GetPosition(null);
        if (TryStartContentDrag())
        {
            e.Handled = true;
        }
    }

    private void ContentOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _contentPointer || _contentHandedOff)
        {
            return;
        }

        _contentPosition = e.GetPosition(null);
        e.Handled |= _contentDragging;
        FinishContentGesture();
    }

    private void ContentOnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (e.Pointer == _contentPointer && _contentGestureId == null)
        {
            CancelContentGesture();
        }
    }

    private void OnContentScrollGesture(InputElement sender, ScrollGestureEventArgs e)
    {
        if (_cancelledContentGestures.Contains(e.Id))
        {
            e.Handled = true;
            e.ShouldEndScrollGesture = true;
            return;
        }
        if (_canvas == null || _contentPointer == null || _contentGestureId.HasValue && _contentGestureId != e.Id)
        {
            return;
        }

        _contentGestureId = e.Id;
        _contentGestureTarget = sender;
        if (_contentClosing)
        {
            UpdateContentClosing(e);
            return;
        }
        if (!_contentDragging && (_contentPosition.Y >= _contentStart.Y || IsFullScreen || !IsFullScreenGestureEnabled))
        {
            if (sender == _backgroundElement)
            {
                e.Handled = true;
                ForwardContentGesture(sender, e, e.Delta);
            }
            return;
        }

        if (!_contentHandedOff)
        {
            TryStartContentDrag();
            if (_contentPointer == null)
            {
                return;
            }

            e.Handled = true;
            if (!_contentDragging)
            {
                return;
            }

            SampleDragPosition(_contentPosition.Y);
            var height = _startVisibleHeight + _contentStart.Y - _contentPosition.Y;
            _dragVisibleHeight = Math.Clamp(height, GetNormalHeight(), _canvas.Bounds.Height);
            UpdatePos();
            if (height < _canvas.Bounds.Height)
            {
                return;
            }

            _contentHandedOff = true;
            _updatingContentState = true;
            try
            {
                SetCurrentValue(IsFullScreenProperty, true);
            }
            finally
            {
                _updatingContentState = false;
            }
            _isDragging = false;
            PseudoClasses.Set(":dragging", false);
            UpdatePos();

            var remaining = Math.Max(0, height - _canvas.Bounds.Height);
            var fraction = e.Delta.Y > 0 ? Math.Min(1, remaining / e.Delta.Y) : 0;
            ForwardContentGesture(sender, e, new Vector(e.Delta.X * fraction, remaining));
        }
        else if (sender == _backgroundElement)
        {
            e.Handled = true;
            ForwardContentGesture(sender, e, e.Delta);
        }
    }

    private void ContentOnUnhandledScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        if (_canvas == null || _workingPanel == null || _contentPointer == null || _contentReleased ||
            _contentDragging || _contentGestureId != e.Id || e.Delta.Y >= 0 ||
            (e.Source as Visual)?.FindAncestorOfType<VerticalDrawer>() != this)
        {
            return;
        }

        var delta = _contentPosition - _contentStart;
        if (delta.Y < 8 || delta.Y <= Math.Abs(delta.X))
        {
            return;
        }

        // Only unhandled gestures reach this handler, after every content control has had its turn.
        // Rebase at the current event so earlier scrolling never contributes to the drawer's movement.
        _contentClosing = _contentDragging = _isDragging = true;
        _contentHandedOff = false;
        _contentStart = _contentPosition + e.Delta;
        _startVisibleHeight = _workingPanel.Bounds.Height + Canvas.GetBottom(_workingPanel);
        _dragSamples.Clear();
        SampleDragPosition(_contentStart.Y);
        PseudoClasses.Set(":dragging", true);
        UpdateContentClosing(e);
    }

    private void UpdateContentClosing(ScrollGestureEventArgs e)
    {
        SampleDragPosition(_contentPosition.Y);
        _dragVisibleHeight = Math.Clamp(_startVisibleHeight + _contentStart.Y - _contentPosition.Y,
            -2, GetMaximumContentDragHeight());
        UpdatePos();
        e.Handled = true;
    }

    private void ForwardContentGesture(InputElement sender, ScrollGestureEventArgs original, Vector delta)
    {
        var target = sender == _backgroundElement ? _contentSource : sender;
        if (target == null || delta == default)
        {
            return;
        }

        _forwardingContentGesture = true;
        try
        {
            _contentForwarded |= sender == _backgroundElement;
            var forwarded = new ScrollGestureEventArgs(original.Id, delta);
            target.RaiseEvent(forwarded);
            original.ShouldEndScrollGesture = forwarded.ShouldEndScrollGesture;
        }
        finally
        {
            _forwardingContentGesture = false;
        }
    }

    private void ForwardContentGestureEnded(InputElement source, int gestureId)
    {
        var forwarding = _forwardingContentGesture;
        _forwardingContentGesture = true;
        try
        {
            source.RaiseEvent(new ScrollGestureEndedEventArgs(gestureId));
        }
        finally
        {
            _forwardingContentGesture = forwarding;
        }
    }

    private void FinishContentGesture()
    {
        if (!_contentDragging || _contentHandedOff || _canvas == null)
        {
            CancelContentGesture();
            return;
        }

        var delta = _contentPosition.Y - _contentStart.Y;
        var velocity = SampleDragPosition(_contentPosition.Y);
        if (_contentClosing)
        {
            var target = GetDragTarget(_dragVisibleHeight, delta, velocity, IsFullScreenGestureEnabled || IsFullScreen);
            CancelContentGesture();
            ApplyDragTarget(target.State, target.FullScreen);
            return;
        }
        var fullScreen = _dragVisibleHeight >= (GetNormalHeight() + _canvas.Bounds.Height) / 2;
        if (Math.Abs(delta) >= 16 && Math.Abs(velocity) >= 800 && Math.Sign(delta) == Math.Sign(velocity))
        {
            fullScreen = velocity < 0;
        }

        CancelContentGesture();
        SetCurrentValue(IsFullScreenProperty, fullScreen);
        UpdatePos();
    }

    private void CancelContentGesture(bool releaseCapture = true)
    {
        if (_updatingContentState || _contentPointer == null)
        {
            return;
        }

        var pointer = _contentPointer;
        var captured = _contentGestureId.HasValue;
        var forwardedSource = _contentForwarded ? _contentSource : null;
        var forwardedGestureId = _contentGestureId;
        if (releaseCapture && _contentGestureId is { } gestureId)
        {
            // A released pointer no longer owns its recognizer, but its inertia can still be running.
            _cancelledContentGestures.Add(gestureId);
        }
        _contentPointer = null;
        _contentSource = null;
        _contentGestureTarget = null;
        _contentGestureId = null;
        _contentInputSubscription?.Dispose();
        _contentInputSubscription = null;
        if (_contentDragging)
        {
            _isDragging = false;
            _dragSamples.Clear();
            PseudoClasses.Set(":dragging", false);
        }
        _contentDragging = _contentClosing = _contentReleased = _contentHandedOff = _contentForwarded = false;
        if (releaseCapture && captured)
        {
            pointer.Capture(null);
        }
        if (forwardedSource != null && forwardedGestureId is { } endedId)
        {
            ForwardContentGestureEnded(forwardedSource, endedId);
        }
        UpdatePos();
    }

    private sealed class ContentScrollGestureRecognizer(VerticalDrawer owner) : ScrollGestureRecognizer
    {
        protected override void PointerMoved(PointerEventArgs e)
        {
            if (!owner._contentDragging && e.Handled && owner._contentPosition.Y > owner._contentStart.Y)
            {
                return;
            }
            base.PointerMoved(e);
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (owner._contentPointer == e.Pointer)
            {
                CanVerticallyScroll = true;
                ScrollStartDistance = 8;
                IsScrollInertiaEnabled = true;
                base.PointerPressed(e);
            }
        }
    }

    private bool CanDragTitle(object? source)
    {
        for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
        {
            if (visual == _titleBar)
            {
                return true;
            }

            if (visual is Control control &&
                (GetIsTitleDragExcluded(control) || control.Focusable ||
                 control is Button or SelectingItemsControl or TextBox or RangeBase or ScrollViewer or
                     SelectableTextBlock or Thumb))
            {
                return false;
            }
        }

        return false;
    }

    private void TitleBarOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isDragging || _workingPanel == null || _canvas == null ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !CanDragTitle(e.Source))
        {
            return;
        }

        CancelDrag();
        _pointer = e.Pointer;
        _pointerStart = e.GetPosition(this);
        _dragSamples.Clear();
        SampleDragPosition(_pointerStart.Y);
        // Read the displayed position so a drag can interrupt an opening/closing transition.
        _startVisibleHeight = _workingPanel.Bounds.Height + Canvas.GetBottom(_workingPanel);
    }

    private void TitleBarOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || _canvas == null)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            CancelDrag();
            return;
        }

        var position = e.GetPosition(this);
        var delta = position - _pointerStart;
        SampleDragPosition(position.Y);
        if (!_isDragging)
        {
            if (Math.Abs(delta.Y) < 8 || Math.Abs(delta.Y) <= Math.Abs(delta.X))
            {
                return;
            }

            _isDragging = true;
            PseudoClasses.Set(":dragging", true);
            e.Pointer.Capture(_titleBar);
        }

        _dragVisibleHeight = Math.Clamp(_startVisibleHeight - delta.Y, -2, _canvas.Bounds.Height);
        UpdatePos();
        e.Handled = true;
    }

    private void TitleBarOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer)
        {
            return;
        }

        var position = e.GetPosition(this);
        var delta = position - _pointerStart;
        if (!_isDragging || _canvas == null)
        {
            CancelDrag();
            if (State == VerticalDrawerOpenState.Collapsed && Math.Abs(delta.X) < 8 && Math.Abs(delta.Y) < 8)
            {
                Open();
            }

            return;
        }

        var velocity = SampleDragPosition(position.Y);
        var height = Math.Clamp(_startVisibleHeight - delta.Y, -2, _canvas.Bounds.Height);
        var target = GetDragTarget(height, delta.Y, velocity);
        EndDrag();
        ApplyDragTarget(target.State, target.FullScreen);
        e.Handled = true;
    }

    private double GetMaximumContentDragHeight() => IsFullScreenGestureEnabled || IsFullScreen
        ? _canvas?.Bounds.Height ?? 0
        : GetNormalHeight();

    private (VerticalDrawerOpenState State, bool FullScreen) GetDragTarget(double height, double delta, double velocity,
        bool allowFullScreen = true)
    {
        var availableHeight = _canvas!.Bounds.Height;
        var stops = new List<(double Height, VerticalDrawerOpenState State, bool FullScreen)>
        {
            (-2, VerticalDrawerOpenState.Closed, false)
        };
        if (AllowCollapse)
        {
            stops.Add((Math.Min(40, availableHeight), VerticalDrawerOpenState.Collapsed, false));
        }
        stops.Add((GetNormalHeight(), VerticalDrawerOpenState.Opened, false));
        if (allowFullScreen)
        {
            stops.Add((availableHeight, VerticalDrawerOpenState.Opened, true));
        }
        var candidates = stops.DistinctBy(stop => stop.Height).ToList();
        var target = candidates
            .OrderBy(stop => Math.Abs(stop.Height - height))
            .ThenBy(stop => delta < 0 ? -stop.Height : stop.Height)
            .First();
        var fling = Math.Abs(delta) >= 16 && Math.Abs(velocity) >= 800 && Math.Sign(delta) == Math.Sign(velocity);
        if (fling)
        {
            var start = candidates.MinBy(stop => Math.Abs(stop.Height - _startVisibleHeight));
            var next = candidates
                .Where(stop => velocity < 0 ? stop.Height > start.Height : stop.Height < start.Height)
                .OrderBy(stop => Math.Abs(stop.Height - start.Height))
                .ToList();
            // A short fling advances at least one stop; longer drags retain their distance-based target.
            if (next.Count > 0 && (velocity < 0 ? target.Height < next[0].Height : target.Height > next[0].Height))
            {
                target = next[0];
            }
        }

        return (target.State, target.FullScreen);
    }

    private void ApplyDragTarget(VerticalDrawerOpenState state, bool fullScreen)
    {
        if (state == VerticalDrawerOpenState.Closed)
        {
            if (!TryClose())
            {
                SetCurrentValue(StateProperty, VerticalDrawerOpenState.Opened);
                UpdatePos();
            }
            return;
        }
        SetCurrentValue(StateProperty, state);
        SetCurrentValue(IsFullScreenProperty, fullScreen);
        UpdatePos();
    }

    private double SampleDragPosition(double y)
    {
        var now = Stopwatch.GetTimestamp();
        _dragSamples.Enqueue((now, y));
        while (_dragSamples.Count > 1 && Stopwatch.GetElapsedTime(_dragSamples.Peek().Time, now).TotalMilliseconds > 100)
        {
            _dragSamples.Dequeue();
        }

        var first = _dragSamples.Peek();
        var seconds = Stopwatch.GetElapsedTime(first.Time, now).TotalSeconds;
        return seconds > 0 ? (y - first.Y) / seconds : 0;
    }

    private void TitleBarOnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_pointer == e.Pointer)
        {
            CancelDrag();
        }
    }

    private void EndDrag()
    {
        var pointer = _pointer;
        _pointer = null;
        _isDragging = false;
        _dragSamples.Clear();
        if (pointer != null && pointer.Captured == _titleBar)
        {
            pointer.Capture(null);
        }
        PseudoClasses.Set(":dragging", false);
    }

    private void CancelDrag()
    {
        CancelContentGesture();
        if (_pointer == null)
        {
            return;
        }

        EndDrag();
        UpdatePos();
    }

    private void CanvasOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        CancelDrag();
        UpdatePos();
    }

    private double GetNormalHeight()
    {
        var availableHeight = _canvas?.Bounds.Height ?? 0;
        if (double.IsFinite(ContentHeight) && ContentHeight > 0)
        {
            return Math.Clamp(ContentHeight, Math.Min(40, availableHeight), availableHeight);
        }
        var ratio = double.IsFinite(ContentHeightRatio) ? Math.Clamp(ContentHeightRatio, 0, 1) : 0.5;
        return Math.Clamp(availableHeight * ratio, Math.Min(40, availableHeight), availableHeight);
    }

    private void UpdatePos()
    {
        if (_canvas == null || _workingPanel == null)
        {
            return;
        }
        
        var normalHeight = GetNormalHeight();
        var visibleHeight = _isDragging ? _dragVisibleHeight : State switch
        {
            VerticalDrawerOpenState.Closed => -2,
            VerticalDrawerOpenState.Collapsed => Math.Min(40, _canvas.Bounds.Height),
            _ => IsFullScreen ? _canvas.Bounds.Height : normalHeight
        };
        SetCurrentValue(ContainerHeightProperty, Math.Max(normalHeight, visibleHeight));
        Canvas.SetBottom(_workingPanel, visibleHeight - ContainerHeight);
    }

    private void UpdateContent()
    {
        PseudoClasses.Set(":opened", State == VerticalDrawerOpenState.Opened);
        PseudoClasses.Set(":collapsed", State == VerticalDrawerOpenState.Collapsed);
        PseudoClasses.Set(":closed", State == VerticalDrawerOpenState.Closed);
        PseudoClasses.Set(":can-collapse", AllowCollapse);
        PseudoClasses.Set(":fullscreen", IsFullScreen);
    }

    private void DismissLayerOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Dismiss();
    }

    public void Close()
    {
        TryClose();
    }

    private bool TryClose()
    {
        if (State == VerticalDrawerOpenState.Closed)
        {
            return true;
        }

        var args = new CancelEventArgs();
        Closing?.Invoke(this, args);
        if (args.Cancel)
        {
            return false;
        }

        CancelDrag();
        SetCurrentValue(StateProperty, VerticalDrawerOpenState.Closed);
        Closed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Collapse()
    {
        if (!AllowCollapse)
        {
            return;
        }

        CancelDrag();
        SetCurrentValue(StateProperty, VerticalDrawerOpenState.Collapsed);
    }

    public void Open()
    {
        CancelDrag();
        SetCurrentValue(IsFullScreenProperty, false);
        SetCurrentValue(StateProperty, VerticalDrawerOpenState.Opened);
    }

    public void Dismiss()
    {
        if (AllowCollapse)
        {
            Collapse();
        }
        else
        {
            Close();
        }
    }
}

public enum VerticalDrawerOpenState
{
    Closed,
    Collapsed,
    Opened
}
