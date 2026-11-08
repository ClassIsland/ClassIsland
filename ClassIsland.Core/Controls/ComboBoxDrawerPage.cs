using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Helpers;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Controls;

internal sealed class ComboBoxDrawerPage : ViewBase
{
    private readonly ComboBox _comboBox;
    private readonly ViewBase _owner;
    private readonly IViewHost _host;
    private readonly Border _contentHost;
    private readonly ScrollViewer _scrollViewer;
    private readonly ItemsPresenter _presenter;
    private readonly IDisposable _popupSuppression;
    private readonly IDisposable _dataContextBinding;
    private bool _opening = true;
    private bool _closing;
    private bool _closed;
    private bool _closeRequested;
    private bool _openedRaised;
    private IPointer? _pressedPointer;
    private Point _pressPosition;

    internal bool IsClosing => _closeRequested || _closed;

    public ComboBoxDrawerPage(ComboBox comboBox, ViewBase owner, IViewHost host, Popup popup,
        Border contentHost, ScrollViewer scrollViewer, ItemsPresenter presenter)
    {
        _comboBox = comboBox;
        _owner = owner;
        _host = host;
        _contentHost = contentHost;
        _scrollViewer = scrollViewer;
        _presenter = presenter;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        FlowDirection = comboBox.FlowDirection;

        // Class handlers run before the template binding. Keep that binding intact for normal popups.
        _popupSuppression = popup.Bind(Popup.IsOpenProperty, Observable.Never<bool>().StartWith(false), BindingPriority.Animation);
        _dataContextBinding = Bind(DataContextProperty, comboBox.GetObservable(DataContextProperty));
        Closed += OnClosed;
        _owner.Closed += OwnerOnClosed;
        _comboBox.PropertyChanged += ComboBoxOnPropertyChanged;
        _comboBox.DetachedFromVisualTree += ComboBoxOnDetachedFromVisualTree;
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, true);
        AddHandler(KeyDownEvent, OnDrawerKeyDown, RoutingStrategies.Bubble, true);
        AddHandler(TextInputEvent, OnDrawerTextInput);
    }

    public void Open() => Dispatcher.UIThread.Post(OpenCore);

    private async void OpenCore()
    {
        try
        {
            if (_closeRequested)
            {
                FinishClose();
                return;
            }

            // Popup normally assigns template parents when it opens; the drawer needs the same item owner.
            AvaloniaUnsafeAccessorHelpers.ApplyTemplatedParent(null, _scrollViewer, _comboBox);
            _comboBox.Classes.Add("drawer-selection");
            _contentHost.Child = null;
            Content = _scrollViewer;
            if (_host.IsDrawerOpen)
            {
                await _host.PushDrawerView(this);
            }
            else
            {
                await _host.ShowDrawer(this);
            }

            _opening = false;
            if (_closed)
            {
                return;
            }
            if (!ReferenceEquals(_host.CurrentDrawerView, this))
            {
                FinishClose();
                return;
            }
            if (_closeRequested)
            {
                RequestClose();
                return;
            }

            _openedRaised = true;
            AvaloniaUnsafeAccessorHelpers.GetComboBoxDropDownOpened(_comboBox)?.Invoke(_comboBox, EventArgs.Empty);
            if (!_closeRequested)
            {
                Dispatcher.UIThread.Post(FocusSelectedItem, DispatcherPriority.Loaded);
            }
        }
        catch (Exception exception)
        {
            IAppHost.TryGetService<ILogger<ComboBoxDrawerPage>>()?.LogError(exception, "无法打开下拉选择抽屉");
            _opening = false;
            if (ReferenceEquals(_host.CurrentDrawerView, this))
            {
                await _host.HideView(this);
            }
            FinishClose();
        }
    }

    public async void RequestClose()
    {
        _closeRequested = true;
        if (_opening || _closing || _closed)
        {
            return;
        }

        _closing = true;
        try
        {
            if (ReferenceEquals(_host.CurrentDrawerView, this))
            {
                await _host.HideView(this);
            }
            if (AssociatedViewHost == null)
            {
                FinishClose();
            }
        }
        finally
        {
            _closing = false;
        }
    }

    private void OnClosed(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, this))
        {
            FinishClose();
        }
    }

    private void OwnerOnClosed(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, _owner))
        {
            RequestClose();
        }
    }

    private void ComboBoxOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TemplateProperty ||
            (e.Property == IsVisibleProperty && !_comboBox.IsVisible) ||
            (e.Property == IsEnabledProperty && !_comboBox.IsEnabled))
        {
            RequestClose();
        }
    }

    private void ComboBoxOnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // A drawer push detaches the previous page without deactivating it.
            if (!_closed && !_comboBox.IsAttachedToVisualTree() &&
                (!_comboBox.GetLogicalAncestors().Contains(_owner) || _owner.AssociatedViewHost != _host ||
                 !_host.DrawerActivatedViews.Contains(_owner)))
            {
                RequestClose();
            }
        });
    }

    private ComboBoxItem? FindItem(object? source)
    {
        var item = (source as Visual)?.FindAncestorOfType<ComboBoxItem>(true);
        return item != null && item.IsEffectivelyEnabled &&
               ItemsControl.ItemsControlFromItemContainer(item) == _comboBox ? item : null;
    }

    private static bool IsItemInteraction(object? source, ComboBoxItem item)
    {
        for (var visual = source as Visual; visual != null && visual != item; visual = visual.GetVisualParent())
        {
            if (visual is InputElement { Focusable: true })
            {
                return false;
            }
        }
        return true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _pressedPointer = e.Pointer;
        _pressPosition = e.GetPosition(_scrollViewer);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var delta = e.GetPosition(_scrollViewer) - _pressPosition;
        if (_pressedPointer == e.Pointer && e.Handled &&
            e.InitialPressMouseButton is MouseButton.Left or MouseButton.Right &&
            delta.X * delta.X + delta.Y * delta.Y < 64 &&
            FindItem(e.Source) is { } item && IsItemInteraction(e.Source, item) &&
            _comboBox.SelectedIndex == _comboBox.IndexFromContainer(item))
        {
            RequestClose();
        }
        _pressedPointer = null;
    }

    private void OnDrawerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            if (e.Key is Key.Enter or Key.Space && FindItem(e.Source) is { } item &&
                IsItemInteraction(e.Source, item) && _comboBox.SelectedIndex == _comboBox.IndexFromContainer(item))
            {
                RequestClose();
            }
            return;
        }

        if (e.Key is Key.Escape or Key.Tab)
        {
            RequestClose();
            e.Handled = true;
            return;
        }

        _comboBox.RaiseEvent(e);
    }

    private void OnDrawerTextInput(object? sender, TextInputEventArgs e)
    {
        if (!e.Handled)
        {
            _comboBox.RaiseEvent(e);
        }
    }

    private void FocusSelectedItem()
    {
        if (_closed || _closeRequested)
        {
            return;
        }
        _presenter.ApplyTemplate();
        if (_comboBox.SelectedIndex >= 0)
        {
            _comboBox.ScrollIntoView(_comboBox.SelectedIndex);
        }
        var item = _comboBox.ContainerFromIndex(_comboBox.SelectedIndex) ??
                   _comboBox.GetRealizedContainers().FirstOrDefault(x => x.IsEffectivelyEnabled);
        item?.Focus();
    }

    private void FinishClose()
    {
        if (_closed)
        {
            return;
        }
        _closed = true;
        _owner.Closed -= OwnerOnClosed;
        _comboBox.PropertyChanged -= ComboBoxOnPropertyChanged;
        _comboBox.DetachedFromVisualTree -= ComboBoxOnDetachedFromVisualTree;
        _comboBox.Classes.Remove("drawer-selection");
        Content = null;
        _contentHost.Child = _scrollViewer;
        _dataContextBinding.Dispose();
        _comboBox.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
        Dispatcher.UIThread.Post(() =>
        {
            _popupSuppression.Dispose();
            ComboBoxAssist.ReleasePage(_comboBox, this);
            if (_openedRaised)
            {
                AvaloniaUnsafeAccessorHelpers.GetComboBoxDropDownClosed(_comboBox)?.Invoke(_comboBox, EventArgs.Empty);
            }
            if (_comboBox.IsAttachedToVisualTree() && _comboBox.IsEffectivelyVisible)
            {
                _comboBox.Focus();
            }
        }, DispatcherPriority.Loaded);
    }
}
