using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Models;

namespace ClassIsland.Controls.ScheduleWeekEdit;

/// <summary>
/// Edits dated course projections through requests, without owning profile data or recurrence rules.
/// </summary>
public sealed class ScheduleWeekEditControl : TemplatedControl
{
    internal const double RulerWidth = 48;
    private const double MinimumDayWidth = 54;
    private const double MinimumBlockHeight = 18;
    public static readonly StyledProperty<IEnumerable<ScheduleWeekOccurrence>?> ItemsSourceProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, IEnumerable<ScheduleWeekOccurrence>?>(nameof(ItemsSource));
    public static readonly StyledProperty<DateOnly> WeekStartProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, DateOnly>(nameof(WeekStart));
    public static readonly StyledProperty<Guid?> SelectedScheduleItemIdProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, Guid?>(nameof(SelectedScheduleItemId), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<DateOnly?> SelectedDateProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, DateOnly?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, TimeSpan?>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, double>(nameof(Scale), 2, validate: value => double.IsFinite(value) && value > 0);
    public static readonly StyledProperty<bool> IsReadonlyProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, bool>(nameof(IsReadonly));

    public IEnumerable<ScheduleWeekOccurrence>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public DateOnly WeekStart { get => GetValue(WeekStartProperty); set => SetValue(WeekStartProperty, value); }
    public Guid? SelectedScheduleItemId { get => GetValue(SelectedScheduleItemIdProperty); set => SetValue(SelectedScheduleItemIdProperty, value); }
    public DateOnly? SelectedDate { get => GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }
    public TimeSpan? SelectedTime { get => GetValue(SelectedTimeProperty); set => SetValue(SelectedTimeProperty, value); }
    /// <summary>Pixels per minute.</summary>
    public double Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }
    public bool IsReadonly { get => GetValue(IsReadonlyProperty); set => SetValue(IsReadonlyProperty, value); }

    public static readonly RoutedEvent<ScheduleWeekEditEventArgs> CreateRequestedEvent =
        RoutedEvent.Register<ScheduleWeekEditControl, ScheduleWeekEditEventArgs>(nameof(CreateRequested), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<ScheduleWeekEditEventArgs> EditRequestedEvent =
        RoutedEvent.Register<ScheduleWeekEditControl, ScheduleWeekEditEventArgs>(nameof(EditRequested), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<ScheduleWeekEditEventArgs> DeleteRequestedEvent =
        RoutedEvent.Register<ScheduleWeekEditControl, ScheduleWeekEditEventArgs>(nameof(DeleteRequested), RoutingStrategies.Bubble);
    public event EventHandler<ScheduleWeekEditEventArgs> CreateRequested { add => AddHandler(CreateRequestedEvent, value); remove => RemoveHandler(CreateRequestedEvent, value); }
    public event EventHandler<ScheduleWeekEditEventArgs> EditRequested { add => AddHandler(EditRequestedEvent, value); remove => RemoveHandler(EditRequestedEvent, value); }
    public event EventHandler<ScheduleWeekEditEventArgs> DeleteRequested { add => AddHandler(DeleteRequestedEvent, value); remove => RemoveHandler(DeleteRequestedEvent, value); }

    private Canvas? _canvas;
    private Canvas? _header;
    private ScrollViewer? _scrollViewer;
    private Canvas? _resizeGuide;
    private Border? _resizeGuideLine;
    private Border? _resizeGuideLabel;
    private TextBlock? _resizeGuideTime;
    private ScheduleWeekRuler? _ruler;
    private readonly List<TextBlock> _dayHeaders = [];
    private readonly Dictionary<(Guid Id, DateOnly Date), ScheduleWeekBlock> _blocks = [];
    private INotifyCollectionChanged? _observedItems;
    private DragState? _drag;
    private bool _isCommittingEdit;
    private bool _attached;
    private bool _initialScrollCompleted;
    private TimeSpan? _pendingScroll;
    private Button? _zoomInButton;
    private Button? _zoomOutButton;
    private Button? _locateButton;
    private Button? _fitButton;
    private int _viewportActionVersion;

    private enum DragKind { Move, Start, End }
    private sealed record DisplayOccurrence(ScheduleWeekOccurrence Source, ScheduleWeekOccurrence Display);
    private sealed class DragState(ScheduleWeekOccurrence item, Point origin, DragKind kind, IPointer pointer)
    {
        public ScheduleWeekOccurrence Item { get; } = item;
        public Point Origin { get; } = origin;
        public DragKind Kind { get; } = kind;
        public IPointer Pointer { get; } = pointer;
        public ScheduleWeekOccurrence Preview { get; set; } = item;
        public bool HasMoved { get; set; }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelDrag();
        if (_canvas != null)
            _canvas.SizeChanged -= CanvasSizeChanged;
        if (_scrollViewer != null)
            _scrollViewer.ScrollChanged -= ScrollChanged;
        if (_zoomInButton != null) _zoomInButton.Click -= ZoomInClicked;
        if (_zoomOutButton != null) _zoomOutButton.Click -= ZoomOutClicked;
        if (_locateButton != null) _locateButton.Click -= LocateClicked;
        if (_fitButton != null) _fitButton.Click -= FitAllClicked;
        base.OnApplyTemplate(e);
        _blocks.Clear();
        _dayHeaders.Clear();
        _ruler = null;
        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _header = e.NameScope.Find<Canvas>("PART_Header");
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _resizeGuide = e.NameScope.Find<Canvas>("PART_ResizeGuide");
        _resizeGuideLine = e.NameScope.Find<Border>("PART_ResizeGuideLine");
        _resizeGuideLabel = e.NameScope.Find<Border>("PART_ResizeGuideLabel");
        _resizeGuideTime = e.NameScope.Find<TextBlock>("PART_ResizeGuideTime");
        _zoomInButton = e.NameScope.Find<Button>("PART_ZoomIn");
        _zoomOutButton = e.NameScope.Find<Button>("PART_ZoomOut");
        _locateButton = e.NameScope.Find<Button>("PART_LocateSelected");
        _fitButton = e.NameScope.Find<Button>("PART_FitAll");
        if (_zoomInButton != null) _zoomInButton.Click += ZoomInClicked;
        if (_zoomOutButton != null) _zoomOutButton.Click += ZoomOutClicked;
        if (_locateButton != null) _locateButton.Click += LocateClicked;
        if (_fitButton != null) _fitButton.Click += FitAllClicked;
        if (_scrollViewer != null)
            _scrollViewer.ScrollChanged += ScrollChanged;
        if (_canvas != null)
            _canvas.SizeChanged += CanvasSizeChanged;
        Rebuild();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        ObserveItems();
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        _viewportActionVersion++;
        CancelDrag();
        if (_observedItems != null)
            _observedItems.CollectionChanged -= ItemsChanged;
        _observedItems = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            CancelDrag();
            ObserveItems();
            Rebuild();
        }
        else if (change.Property == WeekStartProperty || change.Property == ScaleProperty || change.Property == IsReadonlyProperty)
        {
            CancelDrag();
            Rebuild();
        }
        else if (change.Property == SelectedScheduleItemIdProperty)
        {
            UpdateSelection();
            UpdateZoomButtons();
        }
        else if (change.Property == BoundsProperty)
            Rebuild();
        else if (change.Property == IsVisibleProperty && !IsVisible)
            CancelDrag();
    }

    private void ObserveItems()
    {
        if (_observedItems != null)
            _observedItems.CollectionChanged -= ItemsChanged;
        _observedItems = _attached ? ItemsSource as INotifyCollectionChanged : null;
        if (_observedItems != null)
            _observedItems.CollectionChanged += ItemsChanged;
    }

    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) { CancelDrag(); Rebuild(); }
    private void ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_header != null && _scrollViewer != null)
        {
            _header.RenderTransform = new TranslateTransform(-_scrollViewer.Offset.X, 0);
            if (_canvas != null && Math.Abs(_canvas.Width - GetContentWidth(_scrollViewer.Viewport.Width)) > 0.1)
                Rebuild();
            UpdateResizeGuide();
            TryInitialScroll();
        }
    }
    private void CanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Rebuild();
        if (_pendingScroll is { } time)
        {
            _pendingScroll = null;
            ScrollToTime(time);
        }
        TryInitialScroll();
    }

    private void TryInitialScroll()
    {
        if (_initialScrollCompleted || !_attached || _scrollViewer?.Viewport.Height is not > 0 || _canvas?.Bounds.Width is not > 0)
            return;
        _initialScrollCompleted = true;
        var first = VisibleOccurrences().OrderBy(item => item.StartTime).FirstOrDefault();
        ScrollToTime(first == null ? TimeSpan.FromHours(8) : TimeSpan.FromMinutes(Math.Max(0, first.StartTime.TotalMinutes - 30)));
    }

    public void ScrollToTime(TimeSpan time)
    {
        if (_scrollViewer == null || _scrollViewer.Viewport.Height <= 0)
        {
            _pendingScroll = time;
            return;
        }
        _initialScrollCompleted = true;
        _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, Math.Clamp(time.TotalMinutes * Scale, 0,
            Math.Max(0, 1440 * Scale - _scrollViewer.Viewport.Height)));
    }

    private IEnumerable<ScheduleWeekOccurrence> VisibleOccurrences() => (ItemsSource ?? [])
        .Where(item => item.Date.DayNumber >= WeekStart.DayNumber && item.Date.DayNumber - WeekStart.DayNumber < 7);

    private ScheduleWeekOccurrence? SelectedOccurrence() => VisibleOccurrences()
        .Where(item => item.ScheduleItemId == SelectedScheduleItemId)
        .OrderBy(item => item.Date == SelectedDate ? 0 : 1).ThenBy(item => item.Date).FirstOrDefault();

    private void ZoomInClicked(object? sender, RoutedEventArgs e) => ZoomIn();
    private void ZoomOutClicked(object? sender, RoutedEventArgs e) => ZoomOut();
    private void LocateClicked(object? sender, RoutedEventArgs e) => ScrollToSelected();
    private void FitAllClicked(object? sender, RoutedEventArgs e) => FitAll();

    public void ZoomIn()
    {
        if (Scale < 5) ChangeZoom(Math.Min(5, Math.Round(Scale + 0.2, 1)));
    }

    public void ZoomOut()
    {
        if (Scale > 0.2) ChangeZoom(Math.Max(0.2, Math.Round(Scale - 0.2, 1)));
    }

    private void ChangeZoom(double scale)
    {
        var centerMinute = ((_scrollViewer?.Offset.Y ?? 0) + (_scrollViewer?.Viewport.Height ?? 0) / 2) / Scale;
        SetCurrentValue(ScaleProperty, scale);
        Rebuild();
        QueueViewportAction(() =>
        {
            if (SelectedOccurrence() is { } item) CenterOccurrence(item);
            else if (_scrollViewer != null)
                SetViewportOffset(_scrollViewer.Offset.X, centerMinute * Scale - _scrollViewer.Viewport.Height / 2);
        });
    }

    /// <summary>Centers the selected occurrence, preferring its last selected date.</summary>
    public void ScrollToSelected()
    {
        var item = SelectedOccurrence();
        if (item == null && SelectedScheduleItemId == null)
        {
            item = VisibleOccurrences().OrderBy(x => x.StartTime).ThenBy(x => x.Date).FirstOrDefault();
            if (item != null)
            {
                SetCurrentValue(SelectedScheduleItemIdProperty, (Guid?)item.ScheduleItemId);
                SetCurrentValue(SelectedDateProperty, (DateOnly?)item.Date);
            }
        }
        if (item != null) QueueViewportAction(() => CenterOccurrence(item));
    }

    private void CenterOccurrence(ScheduleWeekOccurrence item)
    {
        if (_scrollViewer == null || !_blocks.TryGetValue((item.ScheduleItemId, item.Date), out var block)) return;
        var y = block.Height > _scrollViewer.Viewport.Height
            ? Canvas.GetTop(block) - 20 : Canvas.GetTop(block) + block.Height / 2 - _scrollViewer.Viewport.Height / 2;
        SetViewportOffset(Canvas.GetLeft(block) + block.Width / 2 - _scrollViewer.Viewport.Width / 2, y);
    }

    /// <summary>Fits the displayed week's time range while retaining the minimum day column width.</summary>
    public void FitAll()
    {
        var items = VisibleOccurrences().ToList();
        if (items.Count == 0) { ScrollToTime(TimeSpan.FromHours(8)); return; }
        if (_scrollViewer?.Viewport.Height is not > 0) return;
        CancelDrag();
        var start = items.Min(item => item.StartTime.TotalMinutes);
        var end = items.Max(item => item.EndTime.TotalMinutes);
        SetCurrentValue(ScaleProperty, Math.Clamp((_scrollViewer.Viewport.Height - 40) / Math.Max(5, end - start), 0.01, 5));
        Rebuild();
        QueueViewportAction(() => SetViewportOffset(0, start * Scale - 20));
    }

    private void QueueViewportAction(Action action)
    {
        _initialScrollCompleted = true;
        var version = ++_viewportActionVersion;
        Dispatcher.UIThread.Post(() =>
        {
            if (_attached && version == _viewportActionVersion) action();
        }, DispatcherPriority.Loaded);
    }

    private void SetViewportOffset(double x, double y)
    {
        if (_scrollViewer == null) return;
        _scrollViewer.Offset = new Vector(Math.Clamp(x, 0, Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width)),
            Math.Clamp(y, 0, Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height)));
    }

    private double GetContentWidth(double viewportWidth) => Math.Max(RulerWidth + MinimumDayWidth * 7, viewportWidth);

    private void UpdateZoomButtons()
    {
        if (_zoomInButton != null) _zoomInButton.IsEnabled = Scale < 5;
        if (_zoomOutButton != null) _zoomOutButton.IsEnabled = Scale > 0.2;
        if (_locateButton != null) _locateButton.IsEnabled = SelectedOccurrence() != null
            || SelectedScheduleItemId == null && VisibleOccurrences().Any();
        if (_fitButton != null) _fitButton.IsEnabled = VisibleOccurrences().Any();
    }

    private void UpdateSelection()
    {
        foreach (var block in _blocks.Values)
        {
            var selected = block.Occurrence.ScheduleItemId == SelectedScheduleItemId;
            block.UpdateSelection(selected, IsReadonly, true);
            block.ZIndex = selected ? 1 : 0;
        }
    }

    private void Rebuild()
    {
        if (_canvas == null || _header == null || _isCommittingEdit) return;
        var height = 1440 * Scale;
        _canvas.Height = height;
        var viewportWidth = _scrollViewer?.Viewport.Width ?? 0;
        _canvas.Width = GetContentWidth(viewportWidth > 0 ? viewportWidth : Bounds.Width);
        var width = _canvas.Bounds.Width;
        if (width <= RulerWidth) return;
        var columnWidth = (width - RulerWidth) / 7;
        if (_ruler == null)
        {
            _ruler = new ScheduleWeekRuler { IsHitTestVisible = false };
            _canvas.Children.Add(_ruler);
        }
        _ruler.Scale = Scale;
        _ruler.Width = width;
        _ruler.Height = height;
        string[] days = ["周一", "周二", "周三", "周四", "周五", "周六", "周日"];
        var items = (ItemsSource ?? []).Select(item =>
        {
            var display = item;
            if (_drag is { HasMoved: true } drag && drag.Item.ScheduleItemId == item.ScheduleItemId)
            {
                var delta = drag.Preview.Date.DayNumber - drag.Item.Date.DayNumber;
                var dateNumber = Math.Clamp(item.Date.DayNumber + delta, 0, DateOnly.MaxValue.DayNumber);
                display = item with { Date = DateOnly.FromDayNumber(dateNumber), StartTime = drag.Preview.StartTime, EndTime = drag.Preview.EndTime };
            }
            return new DisplayOccurrence(item, display);
        }).ToList();
        var activeKeys = new HashSet<(Guid Id, DateOnly Date)>();
        for (var day = 0; day < 7; day++)
        {
            var date = WeekStart.AddDays(day);
            if (_dayHeaders.Count <= day)
            {
                var label = new TextBlock { TextAlignment = TextAlignment.Center, FontSize = 12 };
                _dayHeaders.Add(label);
                _header.Children.Add(label);
            }
            var header = _dayHeaders[day];
            header.Text = $"{days[day]}\n{date:MM/dd}";
            header.Width = columnWidth;
            Canvas.SetLeft(header, RulerWidth + day * columnWidth);
            var dayItems = items.Where(x => x.Display.Date == date).OrderBy(x => x.Display.StartTime).ThenBy(x => x.Source.ScheduleItemId).ToList();
            // Include the minimum visual hit area in collision layout so zero/short courses remain selectable.
            var group = new List<DisplayOccurrence>();
            double groupEnd = -1;
            foreach (var item in dayItems)
            {
                if (group.Count > 0 && item.Display.StartTime.TotalMinutes >= groupEnd)
                {
                    UpdateGroup(group, day, columnWidth, activeKeys);
                    group.Clear();
                    groupEnd = -1;
                }
                group.Add(item);
                groupEnd = Math.Max(groupEnd, VisualEnd(item.Display));
            }
            UpdateGroup(group, day, columnWidth, activeKeys);
        }
        foreach (var key in _blocks.Keys.Where(key => !activeKeys.Contains(key)).ToList())
        {
            // A recurrence can leave the displayed week during a preview. Keep its visual for cancellation.
            if (_drag?.Item.ScheduleItemId == key.Id)
                _blocks[key].IsVisible = false;
            else
            {
                _canvas.Children.Remove(_blocks[key]);
                _blocks.Remove(key);
            }
        }
        UpdateSelection();
        UpdateZoomButtons();
        UpdateResizeGuide();
    }

    private void UpdateResizeGuide()
    {
        if (_resizeGuide == null || _resizeGuideLine == null || _resizeGuideLabel == null || _resizeGuideTime == null)
            return;
        _resizeGuide.IsVisible = false;
        if (_scrollViewer == null || IsReadonly || _drag is not { } drag
            || drag.Kind == DragKind.Move && drag.Preview == drag.Item
            || !_blocks.TryGetValue((drag.Item.ScheduleItemId, drag.Item.Date), out var block))
            return;

        var time = drag.Kind == DragKind.End ? drag.Preview.EndTime : drag.Preview.StartTime;
        // Draw in viewport coordinates so scrolling moves the guide with its edge while the time stays at the left.
        var y = time.TotalMinutes * Scale - _scrollViewer.Offset.Y;
        var right = Canvas.GetLeft(block) + block.Width - _scrollViewer.Offset.X;
        var viewport = _scrollViewer.Viewport;
        if (y < 0 || y > viewport.Height || right <= 0 || viewport.Height < _resizeGuideLabel.Height)
            return;
        Canvas.SetLeft(_resizeGuideLine, 24);
        Canvas.SetTop(_resizeGuideLine, y - 1);
        _resizeGuideLine.Width = Math.Max(0, Math.Min(right, viewport.Width) - 24);
        Canvas.SetLeft(_resizeGuideLabel, 2);
        Canvas.SetTop(_resizeGuideLabel, Math.Clamp(y - 10, 0, viewport.Height - _resizeGuideLabel.Height));
        _resizeGuideTime.Text = FormatTime(time);
        _resizeGuide.IsVisible = true;
    }

    private double VisualEnd(ScheduleWeekOccurrence item) => Math.Max(item.EndTime.TotalMinutes, item.StartTime.TotalMinutes + MinimumBlockHeight / Scale);

    private void UpdateGroup(List<DisplayOccurrence> items, int day, double columnWidth, HashSet<(Guid Id, DateOnly Date)> activeKeys)
    {
        var laneEnds = new List<double>();
        var placed = new List<(DisplayOccurrence Item, int Lane)>();
        foreach (var item in items)
        {
            var lane = laneEnds.FindIndex(end => end <= item.Display.StartTime.TotalMinutes);
            if (lane < 0) { lane = laneEnds.Count; laneEnds.Add(0); }
            laneEnds[lane] = VisualEnd(item.Display);
            placed.Add((item, lane));
        }
        foreach (var (entry, lane) in placed)
        {
            var item = entry.Display;
            var key = (entry.Source.ScheduleItemId, entry.Source.Date);
            activeKeys.Add(key);
            if (!_blocks.TryGetValue(key, out var block))
            {
                block = new ScheduleWeekBlock { Occurrence = item };
                block.UpdateSelection(item.ScheduleItemId == SelectedScheduleItemId, IsReadonly, false);
                _blocks.Add(key, block);
                _canvas!.Children.Add(block);
            }
            block.Occurrence = item;
            block.SubjectName = item.SubjectName;
            block.TimeText = $"{FormatTime(item.StartTime)}–{FormatTime(item.EndTime)}";
            block.Width = Math.Max(1, columnWidth / laneEnds.Count - 4);
            block.Height = Math.Max(MinimumBlockHeight, (item.EndTime - item.StartTime).TotalMinutes * Scale);
            block.IsCompact = block.Height < 44;
            block.IsVisible = true;
            block.Cursor = new Cursor(IsReadonly ? StandardCursorType.Arrow : StandardCursorType.SizeAll);
            var tip = $"{item.SubjectName}\n{item.Date:yyyy-MM-dd} {block.TimeText}";
            AutomationProperties.SetName(block, tip);
            var dragging = _drag?.Item.ScheduleItemId == item.ScheduleItemId;
            if (dragging) ToolTip.SetIsOpen(block, false);
            ToolTip.SetTip(block, dragging ? null : tip);
            Canvas.SetLeft(block, RulerWidth + day * columnWidth + lane * columnWidth / laneEnds.Count + 2);
            Canvas.SetTop(block, Math.Clamp(item.StartTime.TotalMinutes * Scale, 0, Math.Max(0, 1440 * Scale - block.Height)));
        }
    }

    private static string FormatTime(TimeSpan value) => value == TimeSpan.FromDays(1) ? "24:00" : value.ToString(@"hh\:mm");
    private static double Snap(double minutes) => Math.Round(minutes / 5, MidpointRounding.AwayFromZero) * 5;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_canvas == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var source = e.Source as Visual;
        var block = source as ScheduleWeekBlock ?? source?.GetVisualAncestors().OfType<ScheduleWeekBlock>().FirstOrDefault();
        if (block == null && source != _canvas) return;
        Focus();
        var point = e.GetPosition(_canvas);
        if (block != null)
        {
            var item = block.Occurrence;
            SetCurrentValue(SelectedScheduleItemIdProperty, item.ScheduleItemId);
            SetCurrentValue(SelectedDateProperty, (DateOnly?)item.Date);
            SetCurrentValue(SelectedTimeProperty, (TimeSpan?)item.StartTime);
            if (!IsReadonly)
            {
                var kind = GetDragKind(e.GetPosition(block).Y, block.Bounds.Height);
                _drag = new DragState(item, point, kind, e.Pointer);
                Cursor = new Cursor(kind == DragKind.Move ? StandardCursorType.SizeAll : StandardCursorType.SizeNorthSouth);
                ToolTip.SetIsOpen(block, false);
                e.Pointer.Capture(this);
                UpdateResizeGuide();
            }
        }
        else if (point.X >= RulerWidth)
        {
            var date = DateAt(point.X);
            var time = TimeSpan.FromMinutes(Math.Clamp(Snap(point.Y / Scale), 0, 1435));
            SetCurrentValue(SelectedScheduleItemIdProperty, (Guid?)null);
            SetCurrentValue(SelectedDateProperty, (DateOnly?)date);
            SetCurrentValue(SelectedTimeProperty, (TimeSpan?)time);
            if (e.ClickCount == 2 && !IsReadonly)
                RaiseEvent(new ScheduleWeekEditEventArgs(CreateRequestedEvent) { Date = date, OriginalDate = date, StartTime = time });
        }
        e.Handled = true;
    }

    private DateOnly DateAt(double x) => WeekStart.AddDays(Math.Clamp((int)((x - RulerWidth) / Math.Max(1, (_canvas!.Bounds.Width - RulerWidth) / 7)), 0, 6));

    private static DragKind GetDragKind(double y, double height)
    {
        var handleHeight = Math.Min(8, height / 3);
        return y <= handleHeight ? DragKind.Start : y >= height - handleHeight ? DragKind.End : DragKind.Move;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_drag is not { } drag)
        {
            var visual = e.Source as Visual;
            var block = visual as ScheduleWeekBlock ?? visual?.GetVisualAncestors().OfType<ScheduleWeekBlock>().FirstOrDefault();
            if (block != null)
                block.Cursor = new Cursor(IsReadonly ? StandardCursorType.Arrow
                    : GetDragKind(e.GetPosition(block).Y, block.Bounds.Height) == DragKind.Move
                        ? StandardCursorType.SizeAll : StandardCursorType.SizeNorthSouth);
            return;
        }
        if (_canvas == null || IsReadonly) return;
        var point = e.GetPosition(_canvas);
        if (!drag.HasMoved && Math.Abs(point.Y - drag.Origin.Y) < 3 && Math.Abs(point.X - drag.Origin.X) < 3) return;
        var wasMoving = drag.HasMoved;
        drag.HasMoved = true;
        var delta = (point.Y - drag.Origin.Y) / Scale;
        var start = drag.Item.StartTime.TotalMinutes;
        var end = drag.Item.EndTime.TotalMinutes;
        var date = drag.Item.Date;
        switch (drag.Kind)
        {
            case DragKind.Start: start = Math.Clamp(Snap(start + delta), 0, Math.Max(0, end - 5)); end = Math.Max(end, start + 5); break;
            case DragKind.End: end = Math.Clamp(Snap(end + delta), Math.Min(1440, start + 5), 1440); start = Math.Min(start, end - 5); break;
            case DragKind.Move:
                var duration = Math.Clamp(end - start, 0, 1440);
                start = Math.Clamp(Snap(start + delta), 0, 1440 - duration);
                end = start + duration;
                date = DateAt(point.X);
                break;
        }
        var preview = drag.Item with { Date = date, StartTime = TimeSpan.FromMinutes(start), EndTime = TimeSpan.FromMinutes(end) };
        if (wasMoving && drag.Preview == preview) return;
        drag.Preview = preview;
        Rebuild();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_drag is not { } drag) return;
        _drag = null;
        e.Pointer.Capture(null);
        Cursor = null;
        _isCommittingEdit = true;
        try
        {
            if (!IsReadonly && drag.HasMoved && drag.Preview != drag.Item)
            {
                var preview = drag.Preview;
                SetCurrentValue(SelectedDateProperty, (DateOnly?)preview.Date);
                SetCurrentValue(SelectedTimeProperty, (TimeSpan?)preview.StartTime);
                // Keep the preview on screen until the host has committed and supplied its updated projection.
                RaiseEvent(new ScheduleWeekEditEventArgs(EditRequestedEvent)
                {
                    ScheduleItemId = preview.ScheduleItemId, OriginalDate = drag.Item.Date, Date = preview.Date,
                    StartTime = preview.StartTime, EndTime = preview.EndTime
                });
                RemapCommittedBlocks(drag);
            }
        }
        finally
        {
            _isCommittingEdit = false;
            Rebuild();
        }
        e.Handled = true;
    }

    private void RemapCommittedBlocks(DragState drag)
    {
        var preview = drag.Preview;
        var delta = preview.Date.DayNumber - drag.Item.Date.DayNumber;
        if (delta == 0 || ItemsSource?.Any(item => item.ScheduleItemId == preview.ScheduleItemId
                && item.Date == preview.Date && item.StartTime == preview.StartTime && item.EndTime == preview.EndTime) != true)
            return;
        var moved = _blocks.Where(pair => pair.Key.Id == preview.ScheduleItemId).ToList();
        foreach (var (key, _) in moved) _blocks.Remove(key);
        foreach (var (key, block) in moved)
        {
            var dayNumber = key.Date.DayNumber + delta;
            if (dayNumber < 0 || dayNumber > DateOnly.MaxValue.DayNumber)
                _canvas?.Children.Remove(block);
            else
                _blocks.Add((key.Id, DateOnly.FromDayNumber(dayNumber)), block);
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { base.OnPointerCaptureLost(e); CancelDrag(); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) { CancelDrag(); e.Handled = true; }
        else if (e.Key == Key.Delete && !IsReadonly && SelectedScheduleItemId is { } id)
        {
            CancelDrag();
            RaiseEvent(new ScheduleWeekEditEventArgs(DeleteRequestedEvent) { ScheduleItemId = id });
            e.Handled = true;
        }
    }

    private void CancelDrag()
    {
        if (_drag is not { } drag) return;
        _drag = null;
        drag.Pointer.Capture(null);
        Cursor = null;
        Rebuild();
    }
}
