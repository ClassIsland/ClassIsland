using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Controls.UI;

public partial class DrawerNavigationHost : UserControl, IViewHost
{
    private DrawerOptions _options = new();
    private ViewBase? _currentView;
    private IInputElement? _previousFocus;
    private bool _isNavigating;
    private bool _isCheckingClose;
    private bool _isClearing;
    private bool _clearPending;
    private bool _isReleased;
    private readonly HashSet<ViewBase> _activatedViews = [];
    private readonly Dictionary<ViewBase, (object? ActionBar, bool HasNavigationBar)> _pageHeaders = [];

    public DrawerNavigationHost()
    {
        InitializeComponent();
        PART_Drawer.Closing += DrawerOnClosing;
        PART_Drawer.Closed += DrawerOnClosed;
    }

    public VerticalDrawer Drawer => PART_Drawer;

    public NavigationPage NavigationPage => PART_NavigationPage;

    public IViewHost? Owner { get; internal set; }

    public ViewBase? CurrentView => _currentView;

    public IReadOnlyCollection<ViewBase> ActivatedViews => _activatedViews;

    public bool IsOpen => PART_Drawer.State != VerticalDrawerOpenState.Closed;

    public bool CanGoBack => PART_NavigationPage.Pages?.Count() > 1;

    bool IViewHost.CanGoBack => IsOpen && !IsBusy;

    public bool IsDrawerOpen => IsOpen;

    public ViewBase? CurrentDrawerView => CurrentView;

    public IReadOnlyCollection<ViewBase> DrawerActivatedViews => ActivatedViews;

    internal bool IsNavigating => _isNavigating || PART_NavigationPage.IsNavigating;

    private bool IsBusy => IsNavigating || _isCheckingClose || _isClearing;

    public event EventHandler<CancelEventArgs>? ClosingRequested;

    public event EventHandler? Closed;

    public event EventHandler<NavigationEventArgs>? Popped;

    public event EventHandler? CurrentViewChanged;

    public event EventHandler? BackRequested;

    public bool ActivateView(ViewBase view)
    {
        if (_isReleased || IsBusy || _activatedViews.Contains(view) || !view.ViewActivating(this))
        {
            return false;
        }

        _activatedViews.Add(view);
        view.ViewActivated(this);
        return true;
    }

    public bool DeactivateView(ViewBase view)
    {
        if (IsBusy || !_activatedViews.Contains(view) || PART_NavigationPage.NavigationStack.Contains(view) ||
            !view.ViewDeactivating(WindowCloseReason.Undefined, true, true))
        {
            return false;
        }

        return DeactivateViewCore(view);
    }

    public void Show()
    {
        if (IsOpen)
        {
            Activate();
        }
    }

    public void Show(IViewHost owner)
    {
        if (!ReferenceEquals(owner, Owner))
        {
            throw new InvalidOperationException("指定的宿主不是此抽屉的所有者。");
        }

        Show();
    }

    public void Hide() => RequestClose();

    public void Activate()
    {
        Owner?.Activate();
        if (IsOpen)
        {
            PART_NavigationPage.Focus();
        }
    }

    public Task ShowView(ViewBase view, ViewBase? owner = null) => ShowViewCoreAsync(view, null);

    public Task ShowViewModal(ViewBase view, ViewBase owner) => ShowDrawerModal(view);

    public Task ShowViewModal(ViewBase view, Window owner) => ShowDrawerModal(view);

    public Task ShowDrawer(ViewBase view, DrawerOptions? options = null) => ShowViewCoreAsync(view, options);

    public Task PushDrawerView(ViewBase view) => ShowView(view);

    public Task ShowDrawerModal(ViewBase view, DrawerOptions? options = null) =>
        view.ShowAndWaitForDeactivationAsync(() => ShowViewCoreAsync(view, options));

    public Task OpenAsync(ViewBase view, DrawerOptions? options = null) => ShowDrawer(view, options);

    public Task PushAsync(ViewBase view) => PushDrawerView(view);

    private async Task ShowViewCoreAsync(ViewBase view, DrawerOptions? options)
    {
        if (_isReleased || IsBusy)
        {
            throw new InvalidOperationException("当前无法在抽屉中显示视图。");
        }
        if (PART_NavigationPage.NavigationStack.Contains(view))
        {
            throw new InvalidOperationException("视图已在抽屉中显示。");
        }

        var opening = !IsOpen;
        var previousView = CurrentView;
        var activatedHere = ActivateView(view);
        _isNavigating = true;
        try
        {
            if (opening)
            {
                _options = options ?? new DrawerOptions();
                _previousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            }

            PART_ActionPresenter.Content = null;
            CapturePageHeader(view);
            await PART_NavigationPage.PushAsync(view, opening ? null : PART_NavigationPage.PageTransition);
            if (_clearPending || _isReleased)
            {
                return;
            }
            if (!ReferenceEquals(PART_NavigationPage.CurrentPage, view))
            {
                throw new InvalidOperationException("无法在抽屉中显示指定视图。");
            }

            SetCurrentView(view);
            UpdateTitleBar();
            if (opening)
            {
                PART_Drawer.Open();
                // Open 会重置全屏状态，因此在打开后应用页面配置。
                ApplyCurrentOptions();
                PART_NavigationPage.Focus();
            }
            if (!_clearPending && !_isReleased)
            {
                view.ViewShown();
            }
        }
        catch
        {
            if (!_clearPending && !_isReleased)
            {
                PART_ActionPresenter.Content = null;
                SetCurrentView(previousView);
                RestorePageHeader(view);
                if (PART_NavigationPage.NavigationStack.Contains(view))
                {
                    PART_NavigationPage.RemovePage(view);
                }
                if (activatedHere)
                {
                    DeactivateViewCore(view);
                }
                if (opening)
                {
                    PART_Drawer.SetCurrentValue(VerticalDrawer.StateProperty, VerticalDrawerOpenState.Closed);
                    _options = new DrawerOptions();
                    _previousFocus = null;
                }
                UpdateTitleBar();
            }
            throw;
        }
        finally
        {
            FinishNavigation();
        }
    }

    public async Task<bool> PopAsync()
    {
        if (!CanGoBack || IsBusy)
        {
            return false;
        }

        _isNavigating = true;
        try
        {
            PART_ActionPresenter.Content = null;
            var previousView = PART_NavigationPage.CurrentPage;
            await PART_NavigationPage.PopAsync();
            return !ReferenceEquals(PART_NavigationPage.CurrentPage, previousView);
        }
        finally
        {
            FinishNavigation();
            UpdateTitleBar();
        }
    }

    public Task<bool> HideView(ViewBase view)
    {
        if (!_activatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活才能隐藏。");
        }

        return ReferenceEquals(CurrentView, view) ? NavigateDrawerBack() : Task.FromResult(false);
    }

    public Task<bool> NavigateDrawerBack() => CanGoBack && IsOpen ? PopAsync() : HideDrawer();

    public Task<bool> HideDrawer() => Task.FromResult(TryCloseDrawer());

    public void RequestClose() => TryCloseDrawer();

    private bool TryCloseDrawer()
    {
        if (!IsOpen || IsBusy)
        {
            return false;
        }

        PART_Drawer.Close();
        return !IsOpen;
    }

    internal bool CanCloseViews(WindowCloseReason reason, bool isProgrammatic, bool isCancelable) =>
        CheckClosing(reason, isProgrammatic, isCancelable);

    internal void Release(bool notifyClosing = false)
    {
        if (_isReleased)
        {
            return;
        }

        _isReleased = true;
        if (notifyClosing)
        {
            CheckClosing(WindowCloseReason.Undefined, true, false);
        }
        Clear();
    }

    public void Clear()
    {
        if (_isClearing)
        {
            return;
        }

        _isClearing = true;
        try
        {
            SetCurrentView(null);
            _options = new DrawerOptions();
            _previousFocus = null;
            PART_ActionPresenter.Content = null;
            if (IsOpen)
            {
                PART_Drawer.SetCurrentValue(VerticalDrawer.StateProperty, VerticalDrawerOpenState.Closed);
            }

            // RemovePage can query Closing again unless the views have been deactivated.
            foreach (var view in _activatedViews.ToArray())
            {
                DeactivateViewCore(view);
            }
            if (_isNavigating)
            {
                _clearPending = true;
            }
            else
            {
                ClearPages();
            }
            UpdateTitleBar();
        }
        finally
        {
            _isClearing = false;
        }
    }

    private void FinishNavigation()
    {
        try
        {
            if (_clearPending)
            {
                ClearPages();
            }
        }
        finally
        {
            _clearPending = false;
            _isNavigating = false;
        }
    }

    private void ClearPages()
    {
        foreach (var page in PART_NavigationPage.NavigationStack.ToArray())
        {
            PART_NavigationPage.RemovePage(page);
        }
        foreach (var view in _pageHeaders.Keys.ToArray())
        {
            RestorePageHeader(view);
        }
    }

    private bool DeactivateViewCore(ViewBase view)
    {
        if (!_activatedViews.Remove(view))
        {
            return false;
        }

        view.ViewDeactivated();
        return true;
    }

    private void SetCurrentView(ViewBase? view)
    {
        if (_currentView != null)
        {
            _currentView.PropertyChanged -= CurrentViewOnPropertyChanged;
        }

        _currentView = view;
        if (_currentView != null)
        {
            _currentView.PropertyChanged += CurrentViewOnPropertyChanged;
            ApplyCurrentOptions();
        }
    }

    private void CurrentViewOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        ApplyCurrentOptions(e.Property);
    }

    private void ApplyCurrentOptions(AvaloniaProperty? property = null)
    {
        if (_currentView == null || _isClearing || !IsOpen)
        {
            return;
        }
        if (_currentView is ComboBoxDrawerPage && CanGoBack)
        {
            return;
        }

        if (property == null || property == ViewHostAssist.DrawerMaxWidthProperty)
        {
            PART_Drawer.DrawerMaxWidth = _options.IsMaxWidthSet
                ? _options.MaxWidth
                : ViewHostAssist.GetDrawerMaxWidth(_currentView);
        }
        if (property == null || property == ViewHostAssist.DrawerAllowCollapseProperty)
        {
            PART_Drawer.AllowCollapse = _options.IsAllowCollapseSet
                ? _options.AllowCollapse
                : ViewHostAssist.GetDrawerAllowCollapse(_currentView);
        }
        if (property == null || property == ViewHostAssist.DrawerSmokeBackgroundProperty)
        {
            PART_Drawer.SmokeBackground = _options.IsSmokeBackgroundSet
                ? _options.SmokeBackground
                : ViewHostAssist.GetDrawerSmokeBackground(_currentView);
        }
        if (property == null || property == ViewHostAssist.DrawerContentHeightRatioProperty)
        {
            PART_Drawer.ContentHeightRatio = _options.IsContentHeightRatioSet
                ? _options.ContentHeightRatio
                : ViewHostAssist.GetDrawerContentHeightRatio(_currentView);
        }
        if (property == null || property == ViewHostAssist.DrawerContentHeightProperty)
        {
            PART_Drawer.ContentHeight = (_options.IsContentHeightSet
                ? _options.ContentHeight
                : ViewHostAssist.GetDrawerContentHeight(_currentView)) ?? double.NaN;
        }
        if (property == null || property == ViewHostAssist.DrawerIsFullScreenProperty)
        {
            PART_Drawer.IsFullScreen = _options.IsFullScreenSet
                ? _options.IsFullScreen
                : ViewHostAssist.GetDrawerIsFullScreen(_currentView);
        }
    }

    private void UpdateTitleBar()
    {
        PART_BackButton.IsVisible = CanGoBack;
        if (_currentView is ComboBoxDrawerPage)
        {
            PART_TitlePresenter.Content = _currentView.Header;
            PART_ActionPresenter.Content = null;
            return;
        }
        PART_TitlePresenter.Content = _options.Title ?? _currentView?.Header;
        PART_ActionPresenter.Content = _options.TitleBarContent ??
                                      (_currentView != null &&
                                       _pageHeaders.TryGetValue(_currentView, out var header)
                                          ? header.ActionBar
                                          : null);
    }

    private void CapturePageHeader(ViewBase view)
    {
        var actionBar = view.GetValue(NavigationPage.TopCommandBarProperty);
        _pageHeaders[view] = (actionBar, NavigationPage.GetHasNavigationBar(view));
        if (actionBar != null)
        {
            view.SetCurrentValue(NavigationPage.TopCommandBarProperty, null);
        }
        view.SetCurrentValue(NavigationPage.HasNavigationBarProperty, false);
    }

    private void RestorePageHeader(ViewBase view)
    {
        if (_pageHeaders.Remove(view, out var header))
        {
            view.SetCurrentValue(NavigationPage.HasNavigationBarProperty, header.HasNavigationBar);
            if (header.ActionBar != null)
            {
                view.SetCurrentValue(NavigationPage.TopCommandBarProperty, header.ActionBar);
            }
        }
    }

    private async void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var view = CurrentView;
        BackRequested?.Invoke(this, EventArgs.Empty);
        if (ReferenceEquals(CurrentView, view))
        {
            await NavigateDrawerBack();
        }
    }

    private void DrawerOnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = e.Cancel || IsBusy || !CheckClosing(WindowCloseReason.Undefined, true, true, e);
    }

    private bool CheckClosing(WindowCloseReason reason, bool isProgrammatic, bool isCancelable,
        CancelEventArgs? drawerEvent = null)
    {
        if (_isCheckingClose)
        {
            return !isCancelable;
        }

        _isCheckingClose = true;
        try
        {
            var views = PART_NavigationPage.NavigationStack.OfType<ViewBase>().Reverse()
                .Concat(_activatedViews).Distinct().Where(_activatedViews.Contains).ToArray();
            foreach (var view in views)
            {
                if (!view.ViewDeactivating(reason, isProgrammatic, isCancelable) && isCancelable)
                {
                    return false;
                }
            }
            if (drawerEvent != null)
            {
                ClosingRequested?.Invoke(this, drawerEvent);
            }
            return drawerEvent?.Cancel != true;
        }
        finally
        {
            _isCheckingClose = false;
        }
    }

    private void DrawerOnClosed(object? sender, EventArgs e)
    {
        var previousFocus = _previousFocus;
        Clear();
        if (previousFocus is Control control && control.IsAttachedToVisualTree())
        {
            control.Focus();
        }
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void NavigationPage_OnPopped(object? sender, NavigationEventArgs e)
    {
        if (_isClearing || _clearPending || _isReleased)
        {
            return;
        }

        SetCurrentView(PART_NavigationPage.CurrentPage as ViewBase);
        if (e.Page is ViewBase view)
        {
            PART_ActionPresenter.Content = null;
            RestorePageHeader(view);
            DeactivateViewCore(view);
        }
        Popped?.Invoke(this, e);
        UpdateTitleBar();
        CurrentViewChanged?.Invoke(this, EventArgs.Empty);
    }
}
