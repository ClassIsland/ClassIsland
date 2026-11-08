using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Assists;

namespace ClassIsland.Controls;

/// <summary>
/// 布局 Rectangles 主题的主界面行和独立提醒气泡。
/// </summary>
public class RectanglesNotificationPanel : Panel
{
    public static readonly StyledProperty<int> WindowDockingLocationProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, int>(nameof(WindowDockingLocation), 1);

    public static readonly StyledProperty<double> WindowWidthProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, double>(nameof(WindowWidth));

    public static readonly StyledProperty<double> MainWindowScaleProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, double>(nameof(MainWindowScale), 1);

    public static readonly StyledProperty<bool> IsOpenedProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, bool>(nameof(IsOpened));

    public static readonly StyledProperty<bool> IsOverlayProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, bool>(nameof(IsOverlay));

    public static readonly StyledProperty<double> RevealProgressProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, double>(nameof(RevealProgress));

    public static readonly StyledProperty<double> InertiaOffsetProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, double>(nameof(InertiaOffset));

    public static readonly StyledProperty<double> OverlayProgressProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, double>(nameof(OverlayProgress));

    public static readonly StyledProperty<double> NotificationWidthProperty =
        AvaloniaProperty.Register<RectanglesNotificationPanel, double>(nameof(NotificationWidth));

    private static readonly TimeSpan EntranceDuration = TimeSpan.FromMilliseconds(333);
    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(167);
    private static readonly TimeSpan EntranceInertiaDuration = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan ExitInertiaDuration = TimeSpan.FromMilliseconds(501.14);
    private static readonly Easing EntranceEasing = Easing.Parse("0,0,0,1");
    private static readonly Easing ExitEasing = Easing.Parse("1,0,1,1");
    private static readonly Easing SwitchEasing = Easing.Parse("0.65,0,0.35,1");
    private static readonly ConditionalWeakTable<ImplicitAnimationCollection, OffsetAnimationSuppression>
        OffsetAnimationSuppressions = new();
    private static readonly ConditionalWeakTable<Window, WindowRenderSizeReservation> WindowRenderSizeReservations = new();

    private sealed class WindowRenderSizeReservation
    {
        public Dictionary<RectanglesNotificationPanel, int> Panels { get; } = new();
        public double Height { get; set; }
        public IDisposable? HeightLease { get; set; }
        public IDisposable? SizeToContentLease { get; set; }
        public IDisposable? RenderSizeReservationLease { get; set; }
    }

    private sealed class OffsetAnimationSuppression
    {
        public int Count { get; set; }
        public required Action Restore { get; init; }
    }

    private CancellationTokenSource? _revealCancellation;
    private CancellationTokenSource? _inertiaCancellation;
    private CancellationTokenSource? _overlayCancellation;
    private CancellationTokenSource? _widthCancellation;
    private double _targetWidth;
    private bool _isOverlayPresentation;
    private readonly TranslateTransform _inertiaTransform = new();
    private readonly MatrixTransform _revealTransform = new();

    public int WindowDockingLocation
    {
        get => GetValue(WindowDockingLocationProperty);
        set => SetValue(WindowDockingLocationProperty, value);
    }

    public double WindowWidth
    {
        get => GetValue(WindowWidthProperty);
        set => SetValue(WindowWidthProperty, value);
    }

    public double MainWindowScale
    {
        get => GetValue(MainWindowScaleProperty);
        set => SetValue(MainWindowScaleProperty, value);
    }

    public bool IsOpened
    {
        get => GetValue(IsOpenedProperty);
        set => SetValue(IsOpenedProperty, value);
    }

    public bool IsOverlay
    {
        get => GetValue(IsOverlayProperty);
        set => SetValue(IsOverlayProperty, value);
    }

    public double RevealProgress
    {
        get => GetValue(RevealProgressProperty);
        set => SetValue(RevealProgressProperty, value);
    }

    public double OverlayProgress
    {
        get => GetValue(OverlayProgressProperty);
        set => SetValue(OverlayProgressProperty, value);
    }

    public double InertiaOffset
    {
        get => GetValue(InertiaOffsetProperty);
        set => SetValue(InertiaOffsetProperty, value);
    }

    public double NotificationWidth
    {
        get => GetValue(NotificationWidthProperty);
        set => SetValue(NotificationWidthProperty, value);
    }

    private bool IsCentered => WindowDockingLocation is 1 or 4;

    private bool CanAnimate => this.IsAttachedToVisualTree() && IThemeService.AnimationLevel != 0 &&
                               !MainWindowStylesAssist.GetMainWindowInEditMode(this);

    static RectanglesNotificationPanel()
    {
        AffectsMeasure<RectanglesNotificationPanel>(WindowDockingLocationProperty, WindowWidthProperty,
            MainWindowScaleProperty, RevealProgressProperty, OverlayProgressProperty, NotificationWidthProperty,
            IsOverlayProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenedProperty)
        {
            if (IsOpened)
            {
                UpdateOverlayPresentation();
            }
            var animation = AnimateRevealProgress();
            _ = AnimateInertia();
            if (!IsOpened)
            {
                this.FindAncestorOfType<MainWindowLine>()?.RegisterNotificationExitAnimation(animation);
            }
        }
        else if (change.Property == IsOverlayProperty && IsOpened)
        {
            // Removing :overlay-in during exit must not replace the outgoing body with the mask.
            UpdateOverlayPresentation();
        }
        else if (change.Property == InertiaOffsetProperty || change.Property == WindowDockingLocationProperty)
        {
            UpdateInertiaTransform();
        }
        else if (change.Property == MainWindowStylesAssist.MainWindowInEditModeProperty &&
                 MainWindowStylesAssist.GetMainWindowInEditMode(this))
        {
            ResetAnimations();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isOverlayPresentation = IsOverlay;
        SetValue(RevealProgressProperty, IsOpened ? 1 : 0);
        SetValue(OverlayProgressProperty, _isOverlayPresentation ? 1 : 0);
        SetValue(InertiaOffsetProperty, 0);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ResetAnimations();
        base.OnDetachedFromVisualTree(e);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2 || Children[1] is not LayoutTransformControl
            {
                Child: RectanglesNotificationBubble bubble
            } notification)
        {
            return base.MeasureOverride(availableSize);
        }

        var main = Children[0];
        main.Measure(new Size(availableSize.Width, RectanglesNotificationBubble.BodyHeight));

        var scale = double.IsFinite(MainWindowScale) && MainWindowScale > 0 ? MainWindowScale : 1;
        // Window bounds are already in DIPs; only the user's layout scale needs to be removed.
        var maximumWidth = Math.Max(0, WindowWidth * 0.8 / scale);
        var maximumBodyWidth = Math.Max(0, maximumWidth - (IsCentered ? 0 : RectanglesNotificationBubble.TipLength));
        var contentWidth = bubble.MeasureContentWidth(_isOverlayPresentation);
        var targetWidth = Math.Min(maximumBodyWidth, Math.Max(325, contentWidth));
        if (Math.Abs(_targetWidth - targetWidth) > 0.01)
        {
            _targetWidth = targetWidth;
            AnimateProgress(NotificationWidthProperty, targetWidth, EntranceDuration, SwitchEasing,
                ref _widthCancellation, IsOpened && NotificationWidth > 0);
        }

        bubble.BodyWidth = Math.Min(maximumBodyWidth, Math.Max(0, NotificationWidth));
        bubble.WindowDockingLocation = WindowDockingLocation;
        var progress = Math.Clamp(RevealProgress, 0, 1);
        _revealTransform.Matrix = Matrix.CreateScale(progress, progress);
        notification.LayoutTransform = _revealTransform;
        notification.Opacity = progress;
        notification.IsHitTestVisible = IsOpened;
        notification.Measure(Size.Infinity);

        var width = double.IsFinite(availableSize.Width)
            ? availableSize.Width
            : IsCentered
                ? Math.Max(main.DesiredSize.Width, notification.DesiredSize.Width)
                : main.DesiredSize.Width + notification.DesiredSize.Width;
        var height = IsCentered
            ? main.DesiredSize.Height + notification.DesiredSize.Height
            : Math.Max(main.DesiredSize.Height, notification.DesiredSize.Height);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2)
        {
            return base.ArrangeOverride(finalSize);
        }

        var main = Children[0];
        var notification = Children[1];
        var mainSize = main.DesiredSize;
        var notificationSize = notification.DesiredSize;
        double mainX;
        double mainY;
        double notificationX;
        double notificationY;

        if (IsCentered)
        {
            mainX = (finalSize.Width - mainSize.Width) / 2;
            notificationX = (finalSize.Width - notificationSize.Width) / 2;
            mainY = WindowDockingLocation == 1 ? 0 : notificationSize.Height;
            notificationY = WindowDockingLocation == 1 ? mainSize.Height : 0;
        }
        else
        {
            var viewport = GetViewport(finalSize.Width);
            var left = WindowDockingLocation is 0 or 3;
            var totalWidth = mainSize.Width + notificationSize.Width;
            mainX = left
                ? -Math.Max(0, totalWidth - viewport.Right)
                : finalSize.Width - mainSize.Width + Math.Max(0, viewport.Left - (finalSize.Width - totalWidth));
            notificationX = left ? mainX + mainSize.Width : mainX - notificationSize.Width;
            mainY = 0;
            notificationY = (mainSize.Height - notificationSize.Height) / 2;
        }

        main.Arrange(new Rect(new Point(mainX, mainY), mainSize));
        notification.Arrange(new Rect(new Point(notificationX, notificationY), notificationSize));
        // Translate both slots together without moving the viewport used for overflow avoidance.
        main.RenderTransform = _inertiaTransform;
        notification.RenderTransform = _inertiaTransform;
        return finalSize;
    }

    private Rect GetViewport(double fallbackWidth)
    {
        if (TopLevel.GetTopLevel(this) is { } window &&
            window.TranslatePoint(default, this) is { } left &&
            window.TranslatePoint(new Point(window.Bounds.Width, 0), this) is { } right)
        {
            return new Rect(left.X, 0, Math.Max(0, right.X - left.X), 0);
        }

        return new Rect(0, 0, fallbackWidth, 0);
    }

    private void UpdateOverlayPresentation()
    {
        _isOverlayPresentation = IsOverlay;
        AnimateProgress(OverlayProgressProperty, _isOverlayPresentation ? 1 : 0, EntranceDuration, SwitchEasing,
            ref _overlayCancellation);
        InvalidateMeasure();
    }

    private async Task AnimateRevealProgress()
    {
        var target = IsOpened ? 1 : 0;
        var renderSizeReservation = CanAnimate && IsCentered && Math.Abs(RevealProgress - target) >= 0.01 &&
                                    TopLevel.GetTopLevel(this) is MainWindow
                                    {
                                        ViewModel.IsEditMode: false,
                                        ViewModel.IsWindowMode: false,
                                        ViewModel.IsClosing: false
                                    } window
            ? ReserveWindowRenderSize(window)
            : null;
        var hostReservation = renderSizeReservation == null && CanAnimate && IsCentered &&
                              Math.Abs(RevealProgress - target) >= 0.01
            ? this.GetVisualAncestors().OfType<INotificationRenderSizeHost>().FirstOrDefault()
                ?.ReserveRenderHeight(IsOpened
                    ? (RectanglesNotificationBubble.BodyHeight + RectanglesNotificationBubble.TipLength) *
                      (1 - Math.Clamp(RevealProgress, 0, 1)) * MainWindowScale
                    : 0)
            : null;
        var suppressed = CanAnimate && WindowDockingLocation == 1 && Math.Abs(RevealProgress - target) >= 0.01
            ? SuppressFollowingRowAnimations()
            : [];
        try
        {
            await AnimateProgress(RevealProgressProperty, target,
                IsOpened ? EntranceDuration : ExitDuration, IsOpened ? EntranceEasing : ExitEasing,
                ref _revealCancellation);
        }
        finally
        {
            if (suppressed.Count > 0 || renderSizeReservation != null || hostReservation != null)
            {
                // Restore after the final layout update; overlapping reminders keep their own suspension.
                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var animations in suppressed)
                    {
                        if (OffsetAnimationSuppressions.TryGetValue(animations, out var suppression) &&
                            --suppression.Count == 0)
                        {
                            OffsetAnimationSuppressions.Remove(animations);
                            suppression.Restore();
                        }
                    }
                    renderSizeReservation?.Invoke();
                    hostReservation?.Dispose();
                }, DispatcherPriority.Background);
            }
        }
    }

    private Action ReserveWindowRenderSize(Window window)
    {
        var reservation = WindowRenderSizeReservations.GetValue(window, _ => new WindowRenderSizeReservation());
        reservation.RenderSizeReservationLease ??= window.SetValue(
            MainWindowStylesAssist.IsWindowRenderSizeReservedProperty, true, BindingPriority.Animation);
        reservation.SizeToContentLease ??= window.SetValue(Window.SizeToContentProperty, SizeToContent.Manual,
            BindingPriority.Animation);
        reservation.Panels[this] = reservation.Panels.GetValueOrDefault(this) + 1;
        var baseHeight = window.GetBaseValue(HeightProperty);
        var contentHeight = baseHeight.HasValue ? baseHeight.Value : double.NaN;
        if (!double.IsFinite(contentHeight) || contentHeight <= 0)
        {
            contentHeight = window.Content is Control content ? content.DesiredSize.Height : window.Bounds.Height;
        }
        var pendingHeight = reservation.Panels.Keys.Where(panel => panel.IsOpened && panel.IsCentered)
            .Sum(panel => (RectanglesNotificationBubble.BodyHeight + RectanglesNotificationBubble.TipLength) *
                          (1 - Math.Clamp(panel.RevealProgress, 0, 1)) * panel.MainWindowScale);
        var height = Math.Min(window.MaxHeight, Math.Max(window.MinHeight, contentHeight + pendingHeight));
        if (height > reservation.Height)
        {
            reservation.Height = height;
            reservation.HeightLease?.Dispose();
            // Keep the native surface stable while layout still follows the animated bubble height.
            reservation.HeightLease = window.SetValue(HeightProperty, height, BindingPriority.Animation);
        }
        UpdateReservedWindowLayout(window);
        return () =>
        {
            if (--reservation.Panels[this] == 0)
            {
                reservation.Panels.Remove(this);
            }
            if (reservation.Panels.Count == 0)
            {
                WindowRenderSizeReservations.Remove(window);
                reservation.HeightLease?.Dispose();
                reservation.SizeToContentLease?.Dispose();
                reservation.RenderSizeReservationLease?.Dispose();
                UpdateReservedWindowLayout(window);
            }
        };
    }

    private static void UpdateReservedWindowLayout(Window window)
    {
        if (window is MainWindow { ViewModel.IsClosing: true })
        {
            return;
        }
        if (window is MainWindow mainWindow)
        {
            mainWindow.UpdateNotificationRenderSize();
        }
        window.UpdateLayout();
        if (window is MainWindow updatedMainWindow)
        {
            updatedMainWindow.UpdateNotificationRenderSize();
        }
    }

    private List<ImplicitAnimationCollection> SuppressFollowingRowAnimations()
    {
        var suppressed = new List<ImplicitAnimationCollection>();
        for (Control? row = this; row?.GetVisualParent() is Control parent; row = parent)
        {
            if (parent is not StackPanel { Orientation: Orientation.Vertical } rows ||
                rows.FindAncestorOfType<ItemsControl>()?.Name != "RootMainWindowLinesItemsControl")
            {
                continue;
            }

            foreach (var followingRow in rows.Children.Skip(rows.Children.IndexOf(row) + 1))
            {
                if (!WrapPanelResizingAnimationAssist.GetIsAnimationAttached(followingRow) ||
                    ElementComposition.GetElementVisual(followingRow) is not { ImplicitAnimations: { } animations } visual)
                {
                    continue;
                }
                if (!OffsetAnimationSuppressions.TryGetValue(animations, out var suppression))
                {
                    if (!animations.TryGetValue(nameof(CompositionVisual.Offset), out var offsetAnimation))
                    {
                        continue;
                    }
                    suppression = new OffsetAnimationSuppression
                    {
                        Restore = () =>
                        {
                            if (ReferenceEquals(visual.ImplicitAnimations, animations) &&
                                !animations.ContainsKey(nameof(CompositionVisual.Offset)))
                            {
                                animations[nameof(CompositionVisual.Offset)] = offsetAnimation;
                            }
                        }
                    };
                    OffsetAnimationSuppressions.Add(animations, suppression);
                    animations.Remove(nameof(CompositionVisual.Offset));
                    visual.StopAnimation(nameof(CompositionVisual.Offset));
                }
                suppression.Count++;
                suppressed.Add(animations);
            }
            break;
        }
        return suppressed;
    }

    private Task AnimateInertia()
    {
        var from = InertiaOffset;
        CancelAnimation(ref _inertiaCancellation);
        SetValue(InertiaOffsetProperty, 0);
        if (!CanAnimate || IThemeService.AnimationLevel < 2)
        {
            return Task.CompletedTask;
        }

        var duration = IsOpened ? EntranceInertiaDuration : ExitInertiaDuration;
        var animation = new Animation
        {
            Duration = duration,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(InertiaOffsetProperty, from) } }
            }
        };
        animation.Children.Add(new KeyFrame
        {
            Cue = new Cue((IsOpened ? 333.34 : 167.04) / duration.TotalMilliseconds),
            KeySpline = new KeySpline(0, 0, 0, 1),
            Setters = { new Setter(InertiaOffsetProperty, IsOpened ? 4.0 : -2.0) }
        });
        animation.Children.Add(new KeyFrame
        {
            Cue = new Cue(1),
            KeySpline = new KeySpline(0.55, 0.55, 0, 1),
            Setters = { new Setter(InertiaOffsetProperty, 0.0) }
        });
        _inertiaCancellation = new CancellationTokenSource();
        return RunAnimationAsync(animation, _inertiaCancellation.Token);
    }

    private void UpdateInertiaTransform()
    {
        _inertiaTransform.X = WindowDockingLocation switch
        {
            0 or 3 => InertiaOffset,
            1 or 4 => 0,
            _ => -InertiaOffset
        };
        _inertiaTransform.Y = WindowDockingLocation switch
        {
            1 => InertiaOffset,
            4 => -InertiaOffset,
            _ => 0
        };
    }

    private Task AnimateProgress(StyledProperty<double> property, double target, TimeSpan duration, Easing easing,
        ref CancellationTokenSource? cancellation, bool animate = true)
    {
        var from = GetValue(property);
        CancelAnimation(ref cancellation);
        // Keep the resting value below animation priority so removing an animation cannot restore the default.
        SetValue(property, target);
        if (!animate || !CanAnimate || Math.Abs(from - target) < 0.01)
        {
            return Task.CompletedTask;
        }

        cancellation = new CancellationTokenSource();
        var animation = new Animation
        {
            Duration = duration,
            Easing = easing,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(property, from) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(property, target) } }
            }
        };
        return RunAnimationAsync(animation, cancellation.Token);
    }

    private async Task RunAnimationAsync(Animation animation, CancellationToken cancellationToken)
    {
        try
        {
            await animation.RunAsync(this, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ResetAnimations()
    {
        CancelAnimation(ref _revealCancellation);
        CancelAnimation(ref _inertiaCancellation);
        CancelAnimation(ref _overlayCancellation);
        CancelAnimation(ref _widthCancellation);
        SetValue(RevealProgressProperty, IsOpened ? 1 : 0);
        SetValue(OverlayProgressProperty, _isOverlayPresentation ? 1 : 0);
        SetValue(NotificationWidthProperty, _targetWidth);
        SetValue(InertiaOffsetProperty, 0);
    }

    private static void CancelAnimation(ref CancellationTokenSource? cancellation)
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }
}
