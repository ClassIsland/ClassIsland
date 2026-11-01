using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.UI;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Controls.UI;

public partial class DrawerNavigationHost : UserControl
{
    private DrawerOptions _options = new();
    private ViewBase? _currentView;
    private bool _isClearing;
    private readonly Dictionary<ViewBase, object?> _pageActionBars = [];

    public DrawerNavigationHost()
    {
        InitializeComponent();
        PART_Drawer.Closing += DrawerOnClosing;
        PART_Drawer.Closed += DrawerOnClosed;
    }

    public VerticalDrawer Drawer => PART_Drawer;

    public NavigationPage NavigationPage => PART_NavigationPage;

    public ViewBase? CurrentView => _currentView;

    public bool IsOpen => PART_Drawer.State != VerticalDrawerOpenState.Closed;

    public bool CanGoBack => PART_NavigationPage.Pages?.Count() > 1;

    public event EventHandler<CancelEventArgs>? ClosingRequested;

    public event EventHandler? Closed;

    public event EventHandler<NavigationEventArgs>? Popped;

    public event EventHandler? CurrentViewChanged;

    public event EventHandler? BackRequested;

    public async Task OpenAsync(ViewBase view, DrawerOptions? options = null)
    {
        _options = options ?? new DrawerOptions();
        await PushAsync(view);
        if (!ReferenceEquals(_currentView, view))
        {
            return;
        }

        PART_Drawer.Open();
        // Open 会重置全屏状态，因此在打开后应用页面配置。
        ApplyCurrentOptions();
    }

    public async Task PushAsync(ViewBase view)
    {
        PART_ActionPresenter.Content = null;
        CapturePageActionBar(view);
        NavigationPage.SetHasNavigationBar(view, false);
        await PART_NavigationPage.PushAsync(view,
            PART_NavigationPage.NavigationStack.Count == 0 ? null : PART_NavigationPage.PageTransition);
        if (!ReferenceEquals(PART_NavigationPage.CurrentPage, view))
        {
            if (_pageActionBars.Remove(view, out var actionBar) && actionBar != null)
            {
                view.SetValue(NavigationPage.TopCommandBarProperty, actionBar);
            }
            UpdateTitleBar();
            return;
        }

        view.ViewShown();
        SetCurrentView(view);
        UpdateTitleBar();
    }

    public async Task<bool> PopAsync()
    {
        if (!CanGoBack)
        {
            return false;
        }

        PART_ActionPresenter.Content = null;
        var previousView = PART_NavigationPage.CurrentPage;
        await PART_NavigationPage.PopAsync();
        UpdateTitleBar();
        return !ReferenceEquals(PART_NavigationPage.CurrentPage, previousView);
    }

    public void RequestClose() => PART_Drawer.Close();

    public void Clear()
    {
        _isClearing = true;
        try
        {
            SetCurrentView(null);
            _options = new DrawerOptions();
            foreach (var page in PART_NavigationPage.NavigationStack.ToArray())
            {
                PART_NavigationPage.RemovePage(page);
            }

            _pageActionBars.Clear();
            PART_ActionPresenter.Content = null;
        }
        finally
        {
            _isClearing = false;
        }
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
        PART_TitlePresenter.Content = _options.Title ?? _currentView?.Header;
        PART_ActionPresenter.Content = _options.TitleBarContent ??
                                      (_currentView != null &&
                                       _pageActionBars.TryGetValue(_currentView, out var actionBar)
                                          ? actionBar
                                          : null);
    }

    private void CapturePageActionBar(ViewBase view)
    {
        var actionBar = view.GetValue(NavigationPage.TopCommandBarProperty);
        _pageActionBars[view] = actionBar;
        if (actionBar != null)
        {
            view.ClearValue(NavigationPage.TopCommandBarProperty);
        }
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DrawerOnClosing(object? sender, CancelEventArgs e)
    {
        ClosingRequested?.Invoke(this, e);
    }

    private void DrawerOnClosed(object? sender, EventArgs e)
    {
        SetCurrentView(null);
        _options = new DrawerOptions();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void NavigationPage_OnPopped(object? sender, NavigationEventArgs e)
    {
        if (_isClearing)
        {
            return;
        }

        SetCurrentView(PART_NavigationPage.CurrentPage as ViewBase);
        Popped?.Invoke(this, e);
        UpdateTitleBar();
        CurrentViewChanged?.Invoke(this, EventArgs.Empty);
    }
}
