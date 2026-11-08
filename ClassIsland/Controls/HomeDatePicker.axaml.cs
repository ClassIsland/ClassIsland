using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsland.ViewModels;

namespace ClassIsland.Controls;

public partial class HomeDatePicker : UserControl
{
    public static readonly StyledProperty<bool> IsExpandedPermanentlyProperty =
        AvaloniaProperty.Register<HomeDatePicker, bool>(nameof(IsExpandedPermanently));

    public bool IsExpandedPermanently
    {
        get => GetValue(IsExpandedPermanentlyProperty);
        set => SetValue(IsExpandedPermanentlyProperty, value);
    }

    // Leave room for lessons even when a desktop window is shorter than the expanded calendar.
    public static FuncValueConverter<double, double> AvailableHeightConverter { get; } =
        new(height => height > 0 ? Math.Max(0, height - 100) : double.PositiveInfinity);
    private IPointer? _gesturePointer;
    private Point _gestureStart;
    private DateTime _gestureDate;
    private bool _gestureClaimed;
    private bool _releasingCapture;
    private double _wheelDistance;
    private long _lastWheelTimestamp;
    private long _lastWheelNavigation;

    private MainViewViewModel? ViewModel => DataContext as MainViewViewModel;

    public HomeDatePicker()
    {
        InitializeComponent();
        DateGestureArea.AddHandler(PointerPressedEvent, DatePointerPressed, RoutingStrategies.Tunnel, true);
        DateGestureArea.AddHandler(PointerMovedEvent, DatePointerMoved, RoutingStrategies.Tunnel, true);
        DateGestureArea.AddHandler(PointerReleasedEvent, DatePointerReleased, RoutingStrategies.Tunnel, true);
        DateGestureArea.AddHandler(PointerCaptureLostEvent, DatePointerCaptureLost, RoutingStrategies.Direct, true);
        DateGestureArea.AddHandler(PointerWheelChangedEvent, DatePointerWheelChanged, RoutingStrategies.Tunnel);
        MonthCalendar.PropertyChanged += (_, e) =>
        {
            if (e.Property == Calendar.DisplayModeProperty && MonthCalendar.DisplayMode != CalendarMode.Month)
                MonthCalendar.SetCurrentValue(Calendar.DisplayModeProperty, CalendarMode.Month);
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if ((change.Property == IsExpandedPermanentlyProperty || change.Property == DataContextProperty) && ViewModel != null)
        {
            CancelGesture();
            ViewModel.IsCalendarPinned = IsExpandedPermanently;
        }
    }

    private void DatePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_gesturePointer != null || ViewModel == null || !e.GetCurrentPoint(DateGestureArea).Properties.IsLeftButtonPressed) return;
        _gesturePointer = e.Pointer;
        _gestureStart = e.GetPosition(DateGestureArea);
        _gestureDate = ViewModel.SelectedDate;
        _gestureClaimed = false;
    }

    private void DatePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_gesturePointer != e.Pointer || ViewModel == null) return;
        var delta = e.GetPosition(DateGestureArea) - _gestureStart;
        if (Math.Abs(delta.Y) > Math.Abs(delta.X) &&
            (IsExpandedPermanently || ViewModel.IsMonthCalendarVisible && MonthScrollViewer.Extent.Height > MonthScrollViewer.Viewport.Height + 1))
            return;
        if (!_gestureClaimed && Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) >= 24 &&
            (Math.Abs(delta.X) > Math.Abs(delta.Y) * 1.5 || Math.Abs(delta.Y) > Math.Abs(delta.X) * 1.5))
        {
            _gestureClaimed = true;
            e.Pointer.Capture(DateGestureArea);
            // Native Calendar can select on press. A swipe starts from the date before that press.
            ViewModel.SelectedDate = _gestureDate;
        }
        if (_gestureClaimed) e.Handled = true;
    }

    private void DatePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_gesturePointer != e.Pointer) return;
        var delta = e.GetPosition(DateGestureArea) - _gestureStart;
        var claimed = _gestureClaimed;
        CancelGesture();
        if (!claimed || ViewModel == null) return;
        e.Handled = true;
        if (Math.Abs(delta.X) >= 24 && Math.Abs(delta.X) > Math.Abs(delta.Y) * 1.5)
            Navigate(delta.X < 0);
        else if (!IsExpandedPermanently && Math.Abs(delta.Y) >= 24 && Math.Abs(delta.Y) > Math.Abs(delta.X) * 1.5)
            ViewModel.IsCalendarExpanded = delta.Y > 0;
    }

    private void DatePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_releasingCapture && _gestureClaimed && e.Pointer == _gesturePointer && e.Pointer.Captured != DateGestureArea)
            CancelGesture();
    }

    private void DatePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var horizontal = e.Delta.X;
        if (Math.Abs(horizontal) < 0.01 && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) horizontal = e.Delta.Y;
        if (Math.Abs(horizontal) < 0.01 || Math.Abs(e.Delta.Y) > Math.Abs(horizontal) && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;
        e.Handled = true;
        var now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(_lastWheelTimestamp, now).TotalMilliseconds > 250) _wheelDistance = 0;
        _lastWheelTimestamp = now;
        _wheelDistance += horizontal;
        if (Math.Abs(_wheelDistance) < 1 || Stopwatch.GetElapsedTime(_lastWheelNavigation, now).TotalMilliseconds < 300) return;
        Navigate(_wheelDistance < 0);
        _wheelDistance = 0;
        _lastWheelNavigation = now;
    }

    private void Navigate(bool forward)
    {
        var command = forward ? ViewModel?.NextPeriodCommand : ViewModel?.PreviousPeriodCommand;
        if (command?.CanExecute(null) == true) command.Execute(null);
    }

    private void CancelGesture()
    {
        _releasingCapture = true;
        if (_gesturePointer?.Captured == DateGestureArea) _gesturePointer.Capture(null);
        _gesturePointer = null;
        _gestureClaimed = false;
        _releasingCapture = false;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelGesture();
        base.OnDetachedFromVisualTree(e);
    }
}
