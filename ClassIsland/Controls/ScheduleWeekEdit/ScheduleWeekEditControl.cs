using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Icons;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.ScheduleWeekEdit;

/// <summary>
/// Displays dated courses and sends edit requests without owning profile data or recurrence rules.
/// </summary>
public sealed class ScheduleWeekEditControl : TemplatedControl
{
    internal const double RulerWidth = 36;
    private const double MinimumDayWidth = 40;
    private const double MinimumBlockHeight = 18;
    private const double MinimumGestureScale = 0.2;
    private const double MaximumGestureScale = 5;
    private const double ZoomTick = 0.1;
    private const double ZoomTolerance = 0.0000001;
    private const double MinimumPinchDistance = 8;
    private static readonly TimeSpan TouchHoldDuration = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan SnapDuration = TimeSpan.FromMilliseconds(200);
    private static readonly Easing SnapEasing = new SplineEasing(0, 0, 0, 1);
    public static readonly StyledProperty<IEnumerable<ScheduleWeekOccurrence>?> ItemsSourceProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, IEnumerable<ScheduleWeekOccurrence>?>(nameof(ItemsSource));
    public static readonly StyledProperty<DateOnly> WeekStartProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, DateOnly>(nameof(WeekStart));
    public static readonly StyledProperty<bool> AutoFetchScheduleItemsProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, bool>(nameof(AutoFetchScheduleItems), true);
    public static readonly StyledProperty<Guid?> SelectedScheduleItemIdProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, Guid?>(nameof(SelectedScheduleItemId), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<DateOnly?> SelectedDateProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, DateOnly?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, TimeSpan?>(nameof(SelectedTime), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, double>(nameof(Scale), 1, validate: value => double.IsFinite(value) && value > 0);
    public static readonly StyledProperty<bool> IsReadonlyProperty =
        AvaloniaProperty.Register<ScheduleWeekEditControl, bool>(nameof(IsReadonly));

    public IEnumerable<ScheduleWeekOccurrence>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public DateOnly WeekStart { get => GetValue(WeekStartProperty); set => SetValue(WeekStartProperty, value); }
    /// <summary>Fetches the displayed week's courses from the active profile when enabled.</summary>
    public bool AutoFetchScheduleItems { get => GetValue(AutoFetchScheduleItemsProperty); set => SetValue(AutoFetchScheduleItemsProperty, value); }
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
    public static readonly RoutedEvent<ScheduleWeekEditEventArgs> DuplicateRequestedEvent =
        RoutedEvent.Register<ScheduleWeekEditControl, ScheduleWeekEditEventArgs>(nameof(DuplicateRequested), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<ScheduleWeekEditEventArgs> ForkAndReplaceRequestedEvent =
        RoutedEvent.Register<ScheduleWeekEditControl, ScheduleWeekEditEventArgs>(nameof(ForkAndReplaceRequested), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<ScheduleWeekEditEventArgs> CourseTappedEvent =
        RoutedEvent.Register<ScheduleWeekEditControl, ScheduleWeekEditEventArgs>(nameof(CourseTapped), RoutingStrategies.Bubble);
    public event EventHandler<ScheduleWeekEditEventArgs> CreateRequested { add => AddHandler(CreateRequestedEvent, value); remove => RemoveHandler(CreateRequestedEvent, value); }
    public event EventHandler<ScheduleWeekEditEventArgs> EditRequested { add => AddHandler(EditRequestedEvent, value); remove => RemoveHandler(EditRequestedEvent, value); }
    public event EventHandler<ScheduleWeekEditEventArgs> DeleteRequested { add => AddHandler(DeleteRequestedEvent, value); remove => RemoveHandler(DeleteRequestedEvent, value); }
    public event EventHandler<ScheduleWeekEditEventArgs> DuplicateRequested { add => AddHandler(DuplicateRequestedEvent, value); remove => RemoveHandler(DuplicateRequestedEvent, value); }
    public event EventHandler<ScheduleWeekEditEventArgs> ForkAndReplaceRequested { add => AddHandler(ForkAndReplaceRequestedEvent, value); remove => RemoveHandler(ForkAndReplaceRequestedEvent, value); }
    public event EventHandler<ScheduleWeekEditEventArgs> CourseTapped { add => AddHandler(CourseTappedEvent, value); remove => RemoveHandler(CourseTappedEvent, value); }
    public static readonly RoutedEvent<ScheduleDayHeaderEventArgs> DayHeaderClickedEvent =
        RoutedEvent.Register<ScheduleWeekEditControl, ScheduleDayHeaderEventArgs>(nameof(DayHeaderClicked), RoutingStrategies.Bubble);
    public event EventHandler<ScheduleDayHeaderEventArgs> DayHeaderClicked { add => AddHandler(DayHeaderClickedEvent, value); remove => RemoveHandler(DayHeaderClickedEvent, value); }

    private Canvas? _canvas;
    private Canvas? _header;
    private ScrollViewer? _scrollViewer;
    private Canvas? _resizeGuide;
    private Border? _resizeGuideLine;
    private Border? _resizeGuideLabel;
    private TextBlock? _resizeGuideTime;
    private ScheduleWeekRuler? _ruler;
    private ScheduleWeekRuler? _headerRuler;
    private readonly List<Button> _dayHeaders = [];
    private readonly List<Run> _dayHeaderDates = [];
    private readonly HashSet<DateOnly> _adjustedDates = [];
    private bool? _compactHeaders;
    private readonly Dictionary<(Guid Id, DateOnly Date), ScheduleWeekBlock> _blocks = [];
    private readonly CompositeDisposable _autoItemSubscriptions = new();
    private IReadOnlyList<ScheduleWeekOccurrence> _autoItems = [];
    private INotifyCollectionChanged? _observedItems;
    private bool _autoRefreshQueued;
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
    private IDisposable? _touchHoldTimer;
    private MenuFlyout? _openBlockMenu;
    private ScheduleWeekBlock? _tapCandidate;
    private Point _tapOrigin;
    private readonly Dictionary<IPointer, Point> _touchPoints = [];
    private PinchState? _pinch;
    private bool _consumeTouches;
    private bool _changingTouchCapture;
    private bool _applyingPinchScale;
    private bool _touchScrolling;

    public ScheduleWeekEditControl()
    {
        AddHandler(PointerPressedEvent, TouchPointerPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerMovedEvent, TouchPointerMoved, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, TouchPointerReleased, RoutingStrategies.Tunnel, true);
        AddHandler(ScrollGestureEvent, (_, _) => _touchScrolling = _touchPoints.Count == 1,
            RoutingStrategies.Bubble, true);
        AddHandler(ScrollGestureInertiaStartingEvent, TouchScrollEnded, RoutingStrategies.Bubble, true);
        AddHandler(ScrollGestureEndedEvent, TouchScrollEnded, RoutingStrategies.Bubble, true);
    }

    private void TouchScrollEnded(object? sender, RoutedEventArgs e)
    {
        // Scroll recognizers receive releases directly; inertia starts after the finger is lifted.
        // Its later end must not clear a new contact that interrupts the inertia.
        if (_touchScrolling && !_consumeTouches) _touchPoints.Clear();
        _touchScrolling = false;
    }

    private sealed class PinchState(IPointer first, IPointer second, double scale, double anchorMinute)
    {
        public IPointer First { get; } = first;
        public IPointer Second { get; } = second;
        public double InitialScale { get; } = Math.Clamp(scale, MinimumGestureScale, MaximumGestureScale);
        public double AnchorMinute { get; } = anchorMinute;
        public double PreviewScale { get; set; } = scale;
        public MatrixTransform Transform { get; } = new();
        public bool IsSnapping { get; set; }
        public double InitialDistance { get; set; }
        public double CenterY { get; set; }
    }

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
        CancelPinch();
        CancelDrag();
        if (_canvas != null)
        {
            _canvas.SizeChanged -= CanvasSizeChanged;
            _canvas.PropertyChanged -= CanvasPropertyChanged;
        }
        if (_scrollViewer != null)
            _scrollViewer.ScrollChanged -= ScrollChanged;
        if (_zoomInButton != null) _zoomInButton.Click -= ZoomInClicked;
        if (_zoomOutButton != null) _zoomOutButton.Click -= ZoomOutClicked;
        if (_locateButton != null) _locateButton.Click -= LocateClicked;
        if (_fitButton != null) _fitButton.Click -= FitAllClicked;
        base.OnApplyTemplate(e);
        _blocks.Clear();
        _dayHeaders.Clear();
        _dayHeaderDates.Clear();
        _compactHeaders = null;
        _ruler = null;
        _headerRuler = null;
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
        {
            _canvas.SizeChanged += CanvasSizeChanged;
            _canvas.PropertyChanged += CanvasPropertyChanged;
        }
        Rebuild();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        ObserveItems();
        if (AutoFetchScheduleItems) RefreshAutomaticItems();
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        _viewportActionVersion++;
        CancelPinch();
        CancelDrag();
        _openBlockMenu?.Hide();
        _openBlockMenu = null;
        if (_observedItems != null)
            _observedItems.CollectionChanged -= ItemsChanged;
        _observedItems = null;
        _autoItemSubscriptions.Clear();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            ObserveItems();
            if (!AutoFetchScheduleItems)
            {
                CancelDrag();
                Rebuild();
            }
        }
        else if (change.Property == AutoFetchScheduleItemsProperty)
        {
            CancelDrag();
            ObserveItems();
            if (AutoFetchScheduleItems) RefreshAutomaticItems();
            else
            {
                _autoItemSubscriptions.Clear();
                _autoItems = [];
                _adjustedDates.Clear();
            }
            Rebuild();
        }
        else if (change.Property == WeekStartProperty)
        {
            CancelPinch();
            CancelDrag();
            if (AutoFetchScheduleItems) RefreshAutomaticItems();
            Rebuild();
        }
        else if (change.Property == ScaleProperty || change.Property == IsReadonlyProperty)
        {
            if (!_applyingPinchScale) CancelPinch();
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
        else if (change.Property == TemplateProperty)
        {
            CancelPinch();
            CancelDrag();
        }
        else if (change.Property == IsVisibleProperty && !IsVisible)
        {
            CancelPinch();
            CancelDrag();
        }
    }

    private void ObserveItems()
    {
        if (_observedItems != null)
            _observedItems.CollectionChanged -= ItemsChanged;
        _observedItems = _attached && !AutoFetchScheduleItems ? ItemsSource as INotifyCollectionChanged : null;
        if (_observedItems != null)
            _observedItems.CollectionChanged += ItemsChanged;
    }

    private void QueueAutomaticRefresh()
    {
        if (!_attached || !AutoFetchScheduleItems || _autoRefreshQueued) return;
        _autoRefreshQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _autoRefreshQueued = false;
            if (_attached && AutoFetchScheduleItems) RefreshAutomaticItems();
        });
    }

    private void RefreshAutomaticItems()
    {
        if (!_attached || !AutoFetchScheduleItems) return;

        _autoItemSubscriptions.Clear();
        var profileService = IAppHost.GetService<IProfileService>();
        var lessonsService = IAppHost.GetService<ILessonsService>();
        var settingsService = IAppHost.GetService<SettingsService>();
        var profile = profileService.Profile;
        _adjustedDates.Clear();
        _adjustedDates.UnionWith(profile.OrderedSchedulesV2.Keys.Select(DateOnly.FromDateTime));
        PropertyChangedEventHandler profileChanged = (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Profile.Subjects))
                QueueAutomaticRefresh();
        };
        profile.PropertyChanged += profileChanged;
        _autoItemSubscriptions.Add(Disposable.Create(() => profile.PropertyChanged -= profileChanged));
        lessonsService.ScheduleItemsInvalidated += ScheduleItemsInvalidated;
        _autoItemSubscriptions.Add(Disposable.Create(() => lessonsService.ScheduleItemsInvalidated -= ScheduleItemsInvalidated));
        WatchProperties(settingsService);
        if (profileService is INotifyPropertyChanged observableProfileService) WatchProperties(observableProfileService);
        WatchProperties(settingsService.Settings);
        WatchCollection(settingsService.Settings.MultiWeekRotationOffset);
        WatchCollection(profile.Subjects);
        foreach (var subject in profile.Subjects.Values) WatchProperties(subject);

        var occurrences = new List<ScheduleWeekOccurrence>();
        for (var day = 0; day < 7; day++)
        {
            var date = WeekStart.AddDays(day);
            foreach (var (id, item) in lessonsService.GetScheduleItemsByDate(date))
            {
                profile.Subjects.TryGetValue(item.SubjectId, out var subject);
                var name = !string.IsNullOrWhiteSpace(subject?.Name) ? subject.Name : "未指定科目";
                occurrences.Add(new ScheduleWeekOccurrence(id, date, name, item.StartTime, item.EndTime)
                {
                    SubjectColorHex = subject?.ColorHex,
                    SubjectIconExpression = subject?.Icon,
                    IsReplacement = item.HasReplacementTarget,
                    IsEnabled = item.IsEnabled
                });
            }
        }
        _autoItems = occurrences;
        if (SelectedScheduleItemId is { } selectedId && !profile.ScheduleItems.ContainsKey(selectedId))
            SetCurrentValue(SelectedScheduleItemIdProperty, (Guid?)null);
        Rebuild();
    }

    private void ScheduleItemsInvalidated(object? sender, Predicate<DateOnly> isDateAffected)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ScheduleItemsInvalidated(sender, isDateAffected));
            return;
        }
        if (!_attached || !AutoFetchScheduleItems) return;

        for (var day = 0; day < 7; day++)
        {
            if (!isDateAffected(WeekStart.AddDays(day))) continue;
            QueueAutomaticRefresh();
            break;
        }
    }

    private void DayHeaderOnClick(object? sender, RoutedEventArgs e)
    {
        if (IsReadonly || sender is not Button { Tag: DateOnly date } button) return;
        RaiseEvent(new ScheduleDayHeaderEventArgs(DayHeaderClickedEvent, date, button));
        e.Handled = true;
    }

    private void WatchProperties(INotifyPropertyChanged source)
    {
        PropertyChangedEventHandler handler = (_, _) => QueueAutomaticRefresh();
        source.PropertyChanged += handler;
        _autoItemSubscriptions.Add(Disposable.Create(() => source.PropertyChanged -= handler));
    }

    private void WatchCollection(INotifyCollectionChanged source)
    {
        NotifyCollectionChangedEventHandler handler = (_, _) => QueueAutomaticRefresh();
        source.CollectionChanged += handler;
        _autoItemSubscriptions.Add(Disposable.Create(() => source.CollectionChanged -= handler));
    }

    private IEnumerable<ScheduleWeekOccurrence> CurrentItems => AutoFetchScheduleItems ? _autoItems : ItemsSource ?? [];

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

    private void CanvasPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty) UpdatePinchPreview();
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

    private IEnumerable<ScheduleWeekOccurrence> VisibleOccurrences() => CurrentItems
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
        var compactHeaders = columnWidth < 54;
        var headerHeight = compactHeaders ? 36 : 24;
        _header.Height = headerHeight;
        if (_ruler == null)
        {
            _ruler = new ScheduleWeekRuler { IsHitTestVisible = false };
            _canvas.Children.Add(_ruler);
        }
        _ruler.Scale = Scale;
        _ruler.Width = width;
        _ruler.Height = height;
        if (_headerRuler == null)
        {
            _headerRuler = new ScheduleWeekRuler { ShowTimeScale = false, IsHitTestVisible = false, ZIndex = -1 };
            _header.Children.Add(_headerRuler);
        }
        _headerRuler.Width = width;
        _headerRuler.Height = headerHeight;
        string[] days = ["一", "二", "三", "四", "五", "六", "日"];
        if (_compactHeaders != compactHeaders)
        {
            foreach (var header in _dayHeaders)
                _header.Children.Remove(header);
            _dayHeaders.Clear();
            _dayHeaderDates.Clear();
            _compactHeaders = compactHeaders;
        }
        var items = CurrentItems.Select(item =>
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
                var dateRun = new Run();
                dateRun.Classes.Add("l2");
                label.Inlines!.Add(new Run(compactHeaders ? days[day] : $"{days[day]} "));
                if (compactHeaders)
                    label.Inlines.Add(new LineBreak());
                label.Inlines.Add(dateRun);
                _dayHeaderDates.Add(dateRun);
                var button = new Button
                {
                    Content = label, Padding = new Thickness(0), MinWidth = 0, MinHeight = 0,
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                button.Bind(ThemeProperty, new DynamicResourceExtension("TransparentButton"));
                button.Click += DayHeaderOnClick;
                _dayHeaders.Add(button);
                _header.Children.Add(button);
            }
            var header = _dayHeaders[day];
            header.Tag = date;
            header.Height = headerHeight;
            header.IsHitTestVisible = !IsReadonly;
            header.Focusable = !IsReadonly;
            AutomationProperties.SetName(header, $"{date:yyyy年M月d日}，调整课程");
            ToolTip.SetTip(header, $"{date:yyyy年M月d日}，调整课程");
            var adjusted = _adjustedDates.Contains(date);
            var labelContent = (TextBlock)header.Content!;
            labelContent.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension(
                adjusted ? "AccentTextFillColorPrimaryBrush" : "TextFillColorPrimaryBrush"));
            if (adjusted) _dayHeaderDates[day].Classes.Remove("l2");
            else if (!_dayHeaderDates[day].Classes.Contains("l2")) _dayHeaderDates[day].Classes.Add("l2");
            _dayHeaderDates[day].Text = compactHeaders ? $"{date:M/d}" : $"{date:MM/dd}";
            header.Width = columnWidth;
            Canvas.SetLeft(header, RulerWidth + day * columnWidth);
            Canvas.SetTop(header, 0);
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
            block.UpdateSubjectAppearance(item);
            block.TimeText = $"{FormatTime(item.StartTime)}–{FormatTime(item.EndTime)}";
            block.Width = Math.Max(1, columnWidth / laneEnds.Count - 4);
            block.Height = Math.Max(MinimumBlockHeight, (item.EndTime - item.StartTime).TotalMinutes * Scale);
            // Labels need 50px plus 5px on each side before compact margins take over.
            block.IsCompact = block.Height < 44 || block.Width < 60;
            block.ShowTimeText = block.Height >= 44;
            block.IsReplacement = item.IsReplacement;
            block.IsCourseEnabled = item.IsEnabled;
            block.IsVisible = true;
            block.Cursor = new Cursor(IsReadonly || _openBlockMenu != null
                ? StandardCursorType.Arrow : StandardCursorType.SizeAll);
            var tip = $"{item.SubjectName}\n{item.Date:yyyy-MM-dd} {block.TimeText}"
                      + (item.IsReplacement ? "\n临时覆盖课程" : "")
                      + (item.IsEnabled ? "" : "\n已禁用");
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

    private void TouchPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Touch || _scrollViewer == null || _canvas == null) return;
        if (!_consumeTouches && (e.Source is not Visual visual ||
            visual != _scrollViewer && !_scrollViewer.IsVisualAncestorOf(visual) ||
            visual is ScrollBar || visual.GetVisualAncestors().OfType<ScrollBar>().Any()))
            return;

        if (!_consumeTouches && _pinch is { IsSnapping: true }) CancelPinch();
        _touchPoints[e.Pointer] = e.GetPosition(_scrollViewer);
        if (!_consumeTouches && _touchPoints.Count < 2) return;

        _changingTouchCapture = true;
        try
        {
            if (!_consumeTouches)
            {
                _consumeTouches = true;
                CancelDrag();
                _openBlockMenu?.Hide();
                _openBlockMenu = null;
                _pendingScroll = null;
                _initialScrollCompleted = true;
                _viewportActionVersion++;
                var points = _touchPoints.ToArray();
                var centerY = (points[0].Value.Y + points[1].Value.Y) / 2;
                _pinch = new PinchState(points[0].Key, points[1].Key, Scale,
                    (_scrollViewer.Offset.Y + centerY) / Scale)
                {
                    CenterY = centerY
                };
                _canvas.RenderTransformOrigin = RelativePoint.TopLeft;
                _canvas.RenderTransform = _pinch.Transform;
                var distance = Point.Distance(points[0].Value, points[1].Value);
                if (distance >= MinimumPinchDistance) _pinch.InitialDistance = distance;
            }
            // Explicit capture also releases any scroll recognizer holding the first finger.
            foreach (var pointer in _touchPoints.Keys) pointer.Capture(this);
        }
        finally
        {
            _changingTouchCapture = false;
        }
        e.PreventGestureRecognition();
        e.Handled = true;
    }

    private void TouchPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_touchPoints.ContainsKey(e.Pointer) || _scrollViewer == null) return;
        _touchPoints[e.Pointer] = e.GetPosition(_scrollViewer);
        if (!_consumeTouches) return;
        UpdatePinch();
        e.PreventGestureRecognition();
        e.Handled = true;
    }

    private void UpdatePinch()
    {
        if (_pinch is not { IsSnapping: false } pinch) return;
        var first = _touchPoints[pinch.First];
        var second = _touchPoints[pinch.Second];
        pinch.CenterY = (first.Y + second.Y) / 2;
        var distance = Point.Distance(first, second);
        if (pinch.InitialDistance == 0)
        {
            if (distance >= MinimumPinchDistance) pinch.InitialDistance = distance;
            return;
        }
        var scale = Math.Clamp(pinch.InitialScale * distance / pinch.InitialDistance,
            MinimumGestureScale, MaximumGestureScale);
        var previousScale = pinch.PreviewScale;
        pinch.PreviewScale = scale;
        var direction = scale >= previousScale ? 1 : -1;
        var tickCount = 0;
        var committedScale = Scale;
        for (var tick = direction > 0 ? 2 : 50; tick >= 2 && tick <= 50; tick += direction)
        {
            var value = tick * ZoomTick;
            if (previousScale + ZoomTolerance < value && value <= scale + ZoomTolerance ||
                scale - ZoomTolerance <= value && value < previousScale - ZoomTolerance)
            {
                committedScale = value;
                tickCount++;
            }
        }
        if (tickCount > 0) ApplyPinchScale(committedScale, pinch);
        else UpdatePinchPreview();
        for (var tick = 0; tick < tickCount; tick++) PlatformServices.HapticFeedbackService.PlayTick();
    }

    private void UpdatePinchPreview()
    {
        if (_pinch is not { } pinch || _canvas == null || _scrollViewer == null ||
            _canvas.TranslatePoint(default, _scrollViewer) is not { } origin)
            return;

        var offset = Math.Clamp(pinch.AnchorMinute * pinch.PreviewScale - pinch.CenterY, 0,
            Math.Max(0, 1440 * pinch.PreviewScale - _scrollViewer.Viewport.Height));
        // The presenter rounds its offset during arrange; compensate using the actual visual origin.
        var layoutY = origin.Y - pinch.Transform.Matrix.M32;
        pinch.Transform.Matrix = new Matrix(1, 0, 0, pinch.PreviewScale / Scale, 0, -layoutY - offset);
    }

    private void ApplyPinchScale(double scale, PinchState pinch)
    {
        _applyingPinchScale = true;
        try
        {
            SetCurrentValue(ScaleProperty, scale);
        }
        finally
        {
            _applyingPinchScale = false;
        }
        UpdatePinchPreview();
        if (_scrollViewer == null) return;
        // Commit the new extent and anchored offset together, keeping intermediate layout out of rendered frames.
        _scrollViewer.UpdateLayout();
        SetViewportOffset(_scrollViewer.Offset.X, pinch.AnchorMinute * Scale - pinch.CenterY);
        _scrollViewer.UpdateLayout();
        UpdatePinchPreview();
    }

    private void TouchPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_touchPoints.ContainsKey(e.Pointer)) return;
        if (!_consumeTouches)
        {
            _touchPoints.Remove(e.Pointer);
            return;
        }
        if (_scrollViewer != null) _touchPoints[e.Pointer] = e.GetPosition(_scrollViewer);
        if (_pinch is { IsSnapping: false } pinch && (pinch.First == e.Pointer || pinch.Second == e.Pointer))
        {
            UpdatePinch();
            var scale = Math.Clamp(Math.Round(pinch.PreviewScale / ZoomTick + ZoomTolerance, MidpointRounding.AwayFromZero) * ZoomTick,
                MinimumGestureScale, MaximumGestureScale);
            SnapPinch(pinch, scale);
        }
        _touchPoints.Remove(e.Pointer);
        _changingTouchCapture = true;
        try
        {
            e.Pointer.Capture(null);
        }
        finally
        {
            _changingTouchCapture = false;
        }
        _consumeTouches = _touchPoints.Count > 0;
        e.PreventGestureRecognition();
        e.Handled = true;
    }

    private void SnapPinch(PinchState pinch, double scale)
    {
        var from = pinch.PreviewScale;
        var changed = Math.Abs(scale - from) > ZoomTolerance;
        pinch.IsSnapping = true;
        ApplyPinchScale(scale, pinch);
        if (!changed || IThemeService.AnimationLevel < 1 || IThemeService.IsTransientDisabled ||
            TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            Complete();
            return;
        }

        TimeSpan? started = null;
        topLevel.RequestAnimationFrame(Animate);

        void Animate(TimeSpan timestamp)
        {
            if (_pinch != pinch) return;
            started ??= timestamp;
            var progress = IThemeService.AnimationLevel < 1 || IThemeService.IsTransientDisabled
                ? 1 : Math.Clamp((timestamp - started.Value).TotalMilliseconds / SnapDuration.TotalMilliseconds, 0, 1);
            pinch.PreviewScale = from + (scale - from) * SnapEasing.Ease(progress);
            UpdatePinchPreview();
            if (progress < 1) topLevel.RequestAnimationFrame(Animate);
            else Complete();
        }

        void Complete()
        {
            pinch.PreviewScale = scale;
            UpdatePinchPreview();
            if (_canvas != null) _canvas.RenderTransform = null;
            _pinch = null;
            if (changed) PlatformServices.HapticFeedbackService.PlayTick();
        }
    }

    private void CancelPinch()
    {
        _viewportActionVersion++;
        if (_pinch != null && _canvas != null) _canvas.RenderTransform = null;
        _pinch = null;
        _consumeTouches = false;
        _touchScrolling = false;
        if (_touchPoints.Count == 0) return;
        var pointers = _touchPoints.Keys.ToArray();
        _touchPoints.Clear();
        _changingTouchCapture = true;
        try
        {
            foreach (var pointer in pointers)
                if (pointer.Captured == this) pointer.Capture(null);
        }
        finally
        {
            _changingTouchCapture = false;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_consumeTouches) return;
        if (_pinch is { IsSnapping: true }) CancelPinch();
        if (_drag != null)
        {
            e.PreventGestureRecognition();
            e.Handled = true;
            return;
        }
        if (_canvas == null) return;
        var source = e.Source as Visual;
        var block = source as ScheduleWeekBlock ?? source?.GetVisualAncestors().OfType<ScheduleWeekBlock>().FirstOrDefault();
        var pointProperties = e.GetCurrentPoint(this).Properties;
        var point = e.GetPosition(_canvas);
        if (pointProperties.IsRightButtonPressed)
        {
            if (IsReadonly) return;
            CancelTouchHold();
            if (block != null)
            {
                Focus();
                SetCurrentValue(SelectedScheduleItemIdProperty, block.Occurrence.ScheduleItemId);
                SetCurrentValue(SelectedDateProperty, (DateOnly?)block.Occurrence.Date);
                SetCurrentValue(SelectedTimeProperty, (TimeSpan?)block.Occurrence.StartTime);
                ShowScheduleItemMenu(block);
                e.Handled = true;
                return;
            }
            if (source != _canvas || point.X < RulerWidth) return;

            var date = DateAt(point.X);
            var time = TimeSpan.FromMinutes(Math.Clamp(Snap(point.Y / Scale), 0, 1435));
            Focus();
            SetCurrentValue(SelectedScheduleItemIdProperty, (Guid?)null);
            SetCurrentValue(SelectedDateProperty, (DateOnly?)date);
            SetCurrentValue(SelectedTimeProperty, (TimeSpan?)time);
            ShowBlankAreaMenu(date, time);
            e.Handled = true;
            return;
        }
        if (!pointProperties.IsLeftButtonPressed) return;
        if (block == null && source != _canvas) return;
        Focus();
        if (block != null)
        {
            var item = block.Occurrence;
            _tapCandidate = block;
            _tapOrigin = point;
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
                // The scroll viewer must not take this touch pointer after the drag begins.
                e.PreventGestureRecognition();
                UpdateResizeGuide();
                if (e.Pointer.Type == PointerType.Touch)
                    StartTouchHold(block);
            }
        }
        else if (point.X >= RulerWidth)
        {
            _tapCandidate = null;
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

    private void ShowScheduleItemMenu(ScheduleWeekBlock block, Action? closed = null)
    {
        if (IsReadonly) return;

        _openBlockMenu?.Hide();
        Cursor = null;
        block.Cursor = new Cursor(StandardCursorType.Arrow);
        var item = block.Occurrence;
        var menu = new MenuFlyout();
        var duplicateItem = new MenuItem
        {
            Header = "复制",
            Icon = new FluentIcon(FluentIcons.CopyRegular) { FontSize = 16 },
            Cursor = new Cursor(StandardCursorType.Arrow)
        };
        duplicateItem.Click += (_, _) =>
        {
            menu.Hide();
            RaiseEvent(new ScheduleWeekEditEventArgs(DuplicateRequestedEvent)
            {
                ScheduleItemId = item.ScheduleItemId,
                OriginalDate = item.Date,
                Date = item.Date,
                StartTime = item.StartTime,
                EndTime = item.EndTime
            });
        };
        var deleteItem = new MenuItem
        {
            Header = "删除",
            Icon = new FluentIcon(FluentIcons.DeleteRegular) { FontSize = 16 },
            Cursor = new Cursor(StandardCursorType.Arrow)
        };
        deleteItem.Click += (_, _) =>
        {
            menu.Hide();
            RaiseEvent(new ScheduleWeekEditEventArgs(DeleteRequestedEvent)
            {
                ScheduleItemId = item.ScheduleItemId,
                OriginalDate = item.Date,
                Date = item.Date,
                StartTime = item.StartTime,
                EndTime = item.EndTime
            });
        };
        var forkItem = new MenuItem
        {
            Header = "分叉并替换",
            Icon = new FluentIcon(FluentIcons.BranchForkRegular) { FontSize = 16 },
            IsEnabled = !item.IsReplacement,
            Cursor = new Cursor(StandardCursorType.Arrow)
        };
        if (item.IsReplacement)
            ToolTip.SetTip(forkItem, "覆盖课程不能再次分叉和替换。");
        forkItem.Click += (_, _) =>
        {
            menu.Hide();
            RaiseEvent(new ScheduleWeekEditEventArgs(ForkAndReplaceRequestedEvent)
            {
                ScheduleItemId = item.ScheduleItemId,
                OriginalDate = item.Date,
                Date = item.Date,
                StartTime = item.StartTime,
                EndTime = item.EndTime
            });
        };
        menu.Items.Add(duplicateItem);
        menu.Items.Add(deleteItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(forkItem);
        _openBlockMenu = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_openBlockMenu, menu))
                _openBlockMenu = null;
            block.Cursor = new Cursor(StandardCursorType.Arrow);
            closed?.Invoke();
        };
        menu.ShowAt(block);
    }

    private void ShowBlankAreaMenu(DateOnly date, TimeSpan time)
    {
        var menu = new MenuFlyout
        {
            Placement = PlacementMode.Pointer
        };
        var addItem = new MenuItem
        {
            Header = "添加",
            Icon = new FluentIcon(FluentIcons.AddRegular) { FontSize = 16 },
            Cursor = new Cursor(StandardCursorType.Arrow)
        };
        addItem.Click += (_, _) =>
        {
            menu.Hide();
            RaiseEvent(new ScheduleWeekEditEventArgs(CreateRequestedEvent)
            {
                Date = date,
                OriginalDate = date,
                StartTime = time
            });
        };
        menu.Items.Add(addItem);
        menu.ShowAt(this);
    }

    private void StartTouchHold(ScheduleWeekBlock block)
    {
        CancelTouchHold();
        _touchHoldTimer = DispatcherTimer.RunOnce(() =>
        {
            _touchHoldTimer = null;
            ShowTouchHoldMenu(block);
        }, TouchHoldDuration);
    }

    private void ShowTouchHoldMenu(ScheduleWeekBlock block)
    {
        if (!_attached || IsReadonly || !block.IsVisible || !block.IsAttachedToVisualTree())
            return;

        CancelDrag();
        SetCurrentValue(SelectedScheduleItemIdProperty, block.Occurrence.ScheduleItemId);
        SetCurrentValue(SelectedDateProperty, (DateOnly?)block.Occurrence.Date);
        SetCurrentValue(SelectedTimeProperty, (TimeSpan?)block.Occurrence.StartTime);
        ToolTip.SetIsOpen(block, false);
        var toolTip = ToolTip.GetTip(block);
        ToolTip.SetTip(block, null);
        ShowScheduleItemMenu(block, () => ToolTip.SetTip(block, toolTip));
    }

    private void CancelTouchHold()
    {
        _touchHoldTimer?.Dispose();
        _touchHoldTimer = null;
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
        if (_consumeTouches) return;
        if (_drag is not { } drag)
        {
            if (_tapCandidate != null && _canvas != null)
            {
                var movedPoint = e.GetPosition(_canvas);
                if (Math.Abs(movedPoint.X - _tapOrigin.X) > 3 || Math.Abs(movedPoint.Y - _tapOrigin.Y) > 3)
                {
                    _tapCandidate = null;
                }
            }
            if (_openBlockMenu != null)
                return;
            var visual = e.Source as Visual;
            var block = visual as ScheduleWeekBlock ?? visual?.GetVisualAncestors().OfType<ScheduleWeekBlock>().FirstOrDefault();
            if (block != null)
                block.Cursor = new Cursor(IsReadonly ? StandardCursorType.Arrow
                    : GetDragKind(e.GetPosition(block).Y, block.Bounds.Height) == DragKind.Move
                        ? StandardCursorType.SizeAll : StandardCursorType.SizeNorthSouth);
            return;
        }
        if (_canvas == null || IsReadonly || e.Pointer != drag.Pointer) return;
        var point = e.GetPosition(_canvas);
        if (_tapCandidate != null &&
            (Math.Abs(point.X - _tapOrigin.X) > 3 || Math.Abs(point.Y - _tapOrigin.Y) > 3))
        {
            _tapCandidate = null;
        }
        if (!drag.HasMoved && Math.Abs(point.Y - drag.Origin.Y) < 3 && Math.Abs(point.X - drag.Origin.X) < 3) return;
        var wasMoving = drag.HasMoved;
        drag.HasMoved = true;
        CancelTouchHold();
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
        if (_consumeTouches) return;
        CancelTouchHold();
        if (_drag is not { } drag)
        {
            if (_tapCandidate != null)
            {
                var point = e.GetPosition(this);
                var canvasPoint = _canvas == null ? point : e.GetPosition(_canvas);
                if (Math.Abs(canvasPoint.X - _tapOrigin.X) <= 3 &&
                    Math.Abs(canvasPoint.Y - _tapOrigin.Y) <= 3)
                {
                    RaiseCourseTapped(_tapCandidate);
                }
                _tapCandidate = null;
                e.Handled = true;
            }
            return;
        }
        if (e.Pointer != drag.Pointer) return;
        var wasTap = !drag.HasMoved && _tapCandidate != null;
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
                if (AutoFetchScheduleItems) RefreshAutomaticItems();
                RemapCommittedBlocks(drag);
            }
            if (wasTap)
            {
                RaiseCourseTapped(drag.Item);
            }
        }
        finally
        {
            _isCommittingEdit = false;
            Rebuild();
        }
        e.Handled = true;
    }

    private void RaiseCourseTapped(ScheduleWeekBlock block)
    {
        RaiseCourseTapped(block.Occurrence);
    }

    private void RaiseCourseTapped(ScheduleWeekOccurrence item)
    {
        RaiseEvent(new ScheduleWeekEditEventArgs(CourseTappedEvent)
        {
            ScheduleItemId = item.ScheduleItemId,
            OriginalDate = item.Date,
            Date = item.Date,
            StartTime = item.StartTime,
            EndTime = item.EndTime
        });
    }

    private void RemapCommittedBlocks(DragState drag)
    {
        var preview = drag.Preview;
        var delta = preview.Date.DayNumber - drag.Item.Date.DayNumber;
        if (delta == 0 || !CurrentItems.Any(item => item.ScheduleItemId == preview.ScheduleItemId
                && item.Date == preview.Date && item.StartTime == preview.StartTime && item.EndTime == preview.EndTime))
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

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_changingTouchCapture) return;
        if (_consumeTouches && _touchPoints.ContainsKey(e.Pointer)) CancelPinch();
        else if (!_touchScrolling) _touchPoints.Remove(e.Pointer);
        if (_drag?.Pointer == e.Pointer) CancelDrag();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) { CancelPinch(); CancelDrag(); e.Handled = true; }
        else if (e.Key == Key.Delete && !IsReadonly && SelectedScheduleItemId is { } id)
        {
            CancelDrag();
            RaiseEvent(new ScheduleWeekEditEventArgs(DeleteRequestedEvent)
            {
                ScheduleItemId = id, Date = SelectedDate ?? WeekStart
            });
            e.Handled = true;
        }
    }

    private void CancelDrag()
    {
        CancelTouchHold();
        _tapCandidate = null;
        if (_drag is not { } drag) return;
        _drag = null;
        drag.Pointer.Capture(null);
        Cursor = null;
        Rebuild();
    }
}
