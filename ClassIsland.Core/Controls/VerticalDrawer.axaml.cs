using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;

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
        if (change.Property != StateProperty && change.Property != IsFullScreenProperty &&
            change.Property != AllowCollapseProperty && change.Property != ContentHeightRatioProperty &&
            change.Property != RequestedWidthProperty)
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
            _dismissLayer.PointerPressed -= DismissLayerOnPointerPressed;
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
        
        _dismissLayer = e.NameScope.Find<Border>("PART_DismissLayer");
        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _workingPanel = e.NameScope.Find<Panel>("WorkingPanel");
        _titleBar = e.NameScope.Find<Grid>("PART_TitleBar");

        if (_dismissLayer != null)
        {
            _dismissLayer.PointerPressed += DismissLayerOnPointerPressed;
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

        UpdateContent();
        UpdatePos();
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
        var stops = new List<(double Height, VerticalDrawerOpenState State, bool FullScreen)>
        {
            (-2, VerticalDrawerOpenState.Closed, false)
        };
        if (AllowCollapse)
        {
            stops.Add((Math.Min(40, _canvas.Bounds.Height), VerticalDrawerOpenState.Collapsed, false));
        }
        stops.Add((GetNormalHeight(), VerticalDrawerOpenState.Opened, false));
        stops.Add((_canvas.Bounds.Height, VerticalDrawerOpenState.Opened, true));
        var candidates = stops.DistinctBy(stop => stop.Height).ToList();
        var target = candidates
            .OrderBy(stop => Math.Abs(stop.Height - height))
            .ThenBy(stop => delta.Y < 0 ? -stop.Height : stop.Height)
            .First();
        var fling = Math.Abs(delta.Y) >= 16 && Math.Abs(velocity) >= 800 && Math.Sign(delta.Y) == Math.Sign(velocity);
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

        EndDrag();
        SetCurrentValue(StateProperty, target.State);
        SetCurrentValue(IsFullScreenProperty, target.FullScreen);
        UpdatePos();
        e.Handled = true;
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

    private void DismissLayerOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Dismiss();
    }

    public void Close()
    {
        CancelDrag();
        SetCurrentValue(StateProperty, VerticalDrawerOpenState.Closed);
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
