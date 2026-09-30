using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// A navigation view whose menu moves between a side rail and a bottom bar.
/// </summary>
public class AdaptiveNavigationView : FANavigationView
{
    /// <summary>
    /// Identifies <see cref="IsNarrow"/>.
    /// </summary>
    public static readonly DirectProperty<AdaptiveNavigationView, bool> IsNarrowProperty =
        AvaloniaProperty.RegisterDirect<AdaptiveNavigationView, bool>(nameof(IsNarrow), view => view.IsNarrow, unsetValue: true);

    private bool _isNarrow = true;

    /// <summary>
    /// Whether the current template presents navigation as a bottom bar instead of a side rail.
    /// </summary>
    public bool IsNarrow => _isNarrow;

    private Border? _activeIndicator;
    private Border? _navigationRail;
    private FAItemsRepeater? _menuItemsHost;
    private FANavigationViewItem? _selectedContainer;
    private Point? _lastIndicatorPosition;
    private Size _lastRailSize;
    private Orientation? _lastOrientation;

    public AdaptiveNavigationView()
    {
        SelectionChanged += OnSelectionChanged;
        LayoutUpdated += (_, _) => UpdateSelectionIndicator();
    }

    protected override Type StyleKeyOverride => typeof(AdaptiveNavigationView);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _activeIndicator = e.NameScope.Find<Border>("ActiveSelectionIndicator");
        _navigationRail = e.NameScope.Find<Border>("NavigationRail");
        _menuItemsHost = e.NameScope.Find<FAItemsRepeater>("MenuItemsHost");
        _selectedContainer = null;
        _lastIndicatorPosition = null;
        _lastOrientation = null;
        if (_activeIndicator != null)
            _activeIndicator.IsVisible = false;
    }

    private void OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        _selectedContainer = e.SelectedItemContainer as FANavigationViewItem;
        Dispatcher.UIThread.Post(UpdateSelectionIndicator, DispatcherPriority.Loaded);
    }

    private void UpdateSelectionIndicator()
    {
        if (_menuItemsHost?.Layout is not FAStackLayout layout)
            return;

        SetAndRaise(IsNarrowProperty, ref _isNarrow, layout.Orientation == Orientation.Horizontal);

        if (_activeIndicator == null || _navigationRail == null)
            return;

        if (SelectedItem == null)
        {
            _activeIndicator.IsVisible = false;
            _lastIndicatorPosition = null;
            return;
        }

        var selected = SelectedItem as FANavigationViewItem ??
            _menuItemsHost.GetVisualDescendants()
                .OfType<FANavigationViewItem>()
                .FirstOrDefault(item => item.IsSelected) ?? _selectedContainer;
        if (selected == null || _navigationRail.Bounds.Width <= 0 || _navigationRail.Bounds.Height <= 0)
            return;

        var transform = selected.TransformToVisual(_navigationRail);
        if (transform == null)
            return;

        var itemPosition = transform.Value.Transform(default);
        var orientation = layout.Orientation;
        var position = orientation == Orientation.Vertical
            ? new Point(4, itemPosition.Y + (selected.Bounds.Height - 16) / 2)
            : new Point(itemPosition.X + (selected.Bounds.Width - 16) / 2,
                itemPosition.Y + selected.Bounds.Height - 4);
        var railSize = _navigationRail.Bounds.Size;
        if (_lastIndicatorPosition == position && _lastOrientation == orientation && _lastRailSize == railSize)
            return;

        var animate = _lastIndicatorPosition != null && _lastOrientation == orientation &&
                      _lastRailSize == railSize && IThemeService.AnimationLevel >= 1 &&
                      !IThemeService.IsTransientDisabled;
        Transitions? transitions = _activeIndicator.Transitions;
        if (!animate)
            _activeIndicator.Transitions = null;

        Canvas.SetLeft(_activeIndicator, position.X);
        Canvas.SetTop(_activeIndicator, position.Y);
        _activeIndicator.IsVisible = true;

        if (!animate)
            _activeIndicator.Transitions = transitions;

        _lastIndicatorPosition = position;
        _lastOrientation = orientation;
        _lastRailSize = railSize;
    }
}
