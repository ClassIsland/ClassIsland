using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Controls.UI;

[PseudoClasses(":mobile", ":inlineHeader", ":closed")]
public partial class WindowViewHost : MyWindow, IViewHost
{
    public bool IsMobileMode { get; init; }
    
    private HashSet<ViewBase> ActivatedViewSet { get; } = [];

    public IReadOnlyCollection<ViewBase> ActivatedViews => ActivatedViewSet;

    private bool _isShowed = false;

    private bool _isWindowLoaded = false;

    private TaskCompletionSource? _windowLoadedCompletionSource;

    private bool _hasLoadedInitialContent = false;

    private bool _isClosed = false;

    private bool _isHostContentPreparedForDetach = false;

    private bool _isHostContentDetached = false;

    private bool _isSyncingHostSize = false;

    private bool _isSyncingHostPosition = false;

    private bool _isSyncingHostWindowState = false;

    private ViewBase? _currentView;

    public ViewBase? CurrentView => _currentView;

    private IDisposable? _currentViewHostPositionObserver;

    private IDisposable? _currentViewHostWindowStateObserver;

    private IDisposable? _currentViewUseInlineHeaderObserver;
    
    private IDisposable? _currentViewHostShowAsDialogObserver;
    
    private IDisposable? _currentViewHeaderHeightOverrideObserver;

    private IDisposable? _currentViewHostFeaturesObserver;

    private AvaloniaList<WindowFeatures>? _observedHostFeatures;
    private WindowFeatures _appliedHostFeatures;

    private double _inlineHeaderHeight = 32.0;

    private static readonly TimeSpan ContentLoadingProgressRingFadeOutDuration = TimeSpan.FromMilliseconds(200);

    public static readonly DirectProperty<WindowViewHost, double> InlineHeaderHeightProperty = AvaloniaProperty.RegisterDirect<WindowViewHost, double>(
        nameof(InlineHeaderHeight), o => o.InlineHeaderHeight, (o, v) => o.InlineHeaderHeight = v);

    public double InlineHeaderHeight
    {
        get => _inlineHeaderHeight;
        set => SetAndRaise(InlineHeaderHeightProperty, ref _inlineHeaderHeight, value);
    }

    public WindowViewHost()
    {
        DataContext = this;
        InitializeComponent();
        Closing += OnClosing;
        Closed += OnClosed;
        Loaded += WindowViewHost_OnLoaded;
        PositionChanged += OnPositionChanged;
    }

    private void WindowViewHost_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isWindowLoaded = true;
        _windowLoadedCompletionSource?.TrySetResult();
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        UpdateCurrentViewHostPositionFromWindow();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        PseudoClasses.Set(":closed", true);
        DetachHostContent();
        Content = null;
        DataContext = null;
        MyOwner = null;
    }

    private void DetachHostContent()
    {
        if (_isHostContentDetached)
        {
            return;
        }

        PrepareHostContentForDetach();
        _isHostContentDetached = true;

        foreach (var page in NavigationPage.NavigationStack.ToArray())
        {
            NavigationPage.RemovePage(page);
        }

        NavigationPage.Template = null;
        NavigationPage.ApplyTemplate();
    }

    private void PrepareHostContentForDetach()
    {
        if (_isHostContentPreparedForDetach)
        {
            return;
        }

        _isHostContentPreparedForDetach = true;
        SetCurrentView(null);
        NavigationPage.Popped -= NavigationPage_OnPopped;

        foreach (var icon in NavigationPage.GetVisualDescendants()
                     .OfType<FASymbolIcon>()
                     .Where(x => ReferenceEquals(x.TemplatedParent, NavigationPage)))
        {
            icon.ClearValue(FASymbolIcon.SymbolProperty);
            icon.ClearValue(TextElement.FontSizeProperty);
            icon.ClearValue(TextElement.ForegroundProperty);
        }

        Styles.Clear();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        // Keep the pages in the logical tree until Avalonia has invalidated their styles.
        // Removing them here would skip the normal child detach traversal and break reuse.
        PrepareHostContentForDetach();
        base.OnDetachedFromLogicalTree(e);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        var view = _currentView ?? ActivatedViewSet.LastOrDefault();
        if (view == null)
        {
            return;
        }

        var isCancelable = e.CloseReason is not (WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown);
        if (!view.ViewDeactivating(e.CloseReason, e.IsProgrammatic, isCancelable) && isCancelable)
        {
            e.Cancel = true;
            return;
        }

        foreach (var activatedView in ActivatedViewSet.ToArray())
        {
            if (!ReferenceEquals(activatedView, view))
            {
                activatedView.ViewDeactivating(e.CloseReason, e.IsProgrammatic, false);
            }
            activatedView.ViewDeactivated();
        }

        ActivatedViewSet.Clear();
    }


    private WindowViewHost? MyOwner { get; set; }

    IViewHost? IViewHost.Owner => MyOwner;

    public new void Activate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        base.Activate();
    }

    public bool ActivateView(ViewBase view)
    {
        if (ActivatedViewSet.Contains(view))
        {
            return false;
        }

        if (!view.ViewActivating(this))
        {
            return false;
        }
        ActivatedViewSet.Add(view);
        view.ViewActivated(this);

        return true;
    }

    public bool DeactivateView(ViewBase view)
    {
        if (!ActivatedViewSet.Contains(view))
        {
            return false;
        }

        if (!view.ViewDeactivating(WindowCloseReason.Undefined, true, true))
        {
            return false;
        }
        ActivatedViewSet.Remove(view);
        view.ViewDeactivated();
        
        return true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TopLevel.ClientSizeProperty)
        {
            UpdateCurrentViewHostSizeFromWindow();
        }

        if (change.Property == Window.WindowStateProperty)
        {
            UpdateCurrentViewHostWindowStateFromWindow();
        }
    }

    private void ApplyHostSizeToWindow(ViewBase view)
    {
        if (IsMobileMode || _isSyncingHostSize)
        {
            return;
        }

        if (view.HostWidth <= 0 || view.HostHeight <= 0)
        {
            return;
        }

        _isSyncingHostSize = true;
        try
        {
            SetCurrentValue(WidthProperty, view.HostWidth);
            SetCurrentValue(HeightProperty, view.HostHeight);
        }
        finally
        {
            _isSyncingHostSize = false;
        }
    }

    private void ApplyHostPositionToWindow(ViewBase view)
    {
        if (IsMobileMode || _isSyncingHostPosition)
        {
            return;
        }

        if (!view.IsSet(ViewBase.HostPositionProperty) || Position == view.HostPosition)
        {
            return;
        }

        _isSyncingHostPosition = true;
        try
        {
            Position = view.HostPosition;
        }
        finally
        {
            _isSyncingHostPosition = false;
        }
    }

    private void ApplyHostWindowStateToWindow(ViewBase view)
    {
        if (_isSyncingHostWindowState || !_isShowed)
        {
            return;
        }

        if (!view.IsSet(ViewBase.HostWindowStateProperty) || WindowState == view.HostWindowState)
        {
            return;
        }

        _isSyncingHostWindowState = true;
        try
        {
            WindowState = view.HostWindowState;
        }
        finally
        {
            _isSyncingHostWindowState = false;
        }
    }

    private void ApplyHostBoundsToWindow(ViewBase view)
    {
        if (view.IsSet(ViewBase.HostWindowStateProperty) && view.HostWindowState == WindowState.Normal)
        {
            ApplyHostWindowStateToWindow(view);
        }
        ApplyHostSizeToWindow(view);
        SyncHostPositionWithWindow(view);
        SyncHostWindowStateWithWindow(view);
    }

    private void ApplyViewFeatures(ViewBase view)
    {
        if (IsMobileMode)
        {
            return;
        }
        PseudoClasses.Set(":inlineHeader", view.UseInlineHeader);
        TitleBar.ExtendsContentIntoTitleBar = view.UseInlineHeader;
        ShowAsDialog = view.ShowAsDialog;
        InlineHeaderHeight = view.IsSet(NavigationPage.BarHeightOverrideProperty)
            ? NavigationPage.GetBarHeightOverride(view) ?? 32.0
            : 32.0;
        if (view.UseInlineHeader)
        {
            TitleBar.Height = InlineHeaderHeight;
        }
        else
        {
            TitleBar.Height = 32;
        }
    }

    private void SyncHostPositionWithWindow(ViewBase view)
    {
        if (view.IsSet(ViewBase.HostPositionProperty) && view.ShowedOnce)
        {
            ApplyHostPositionToWindow(view);
        }
        else
        {
            UpdateHostPositionFromWindow(view);
        }
    }

    private void SyncHostWindowStateWithWindow(ViewBase view)
    {
        if (view.IsSet(ViewBase.HostWindowStateProperty))
        {
            ApplyHostWindowStateToWindow(view);
        }
        else
        {
            UpdateHostWindowStateFromWindow(view);
        }
    }

    private void UpdateCurrentViewHostSizeFromWindow()
    {
        if (_isShowed && NavigationPage.CurrentPage is ViewBase view)
        {
            UpdateHostSizeFromWindow(view);
        }
    }

    private void UpdateCurrentViewHostPositionFromWindow()
    {
        if (_isShowed && NavigationPage.CurrentPage is ViewBase view)
        {
            UpdateHostPositionFromWindow(view);
        }
    }

    private void UpdateCurrentViewHostWindowStateFromWindow()
    {
        if (_isShowed && NavigationPage.CurrentPage is ViewBase view)
        {
            UpdateHostWindowStateFromWindow(view);
        }
    }

    private void UpdateHostBoundsFromWindow(ViewBase view)
    {
        UpdateHostSizeFromWindow(view);
        UpdateHostPositionFromWindow(view);
        UpdateHostWindowStateFromWindow(view);
    }

    private void UpdateHostSizeFromWindow(ViewBase view)
    {
        if (IsMobileMode || _isSyncingHostSize)
        {
            return;
        }

        var size = ClientSize;
        
        _isSyncingHostSize = true;
        try
        {
            if (size is { Width: > 0, Height: > 0 })
            {
                view.HostWidth = size.Width;
                view.HostHeight = size.Height;
            }
        }
        finally
        {
            _isSyncingHostSize = false;
        }
    }

    private void UpdateHostPositionFromWindow(ViewBase view)
    {
        if (IsMobileMode || _isSyncingHostPosition || !_isShowed)
        {
            return;
        }

        if (view.IsSet(ViewBase.HostPositionProperty) && view.HostPosition == Position)
        {
            return;
        }

        _isSyncingHostPosition = true;
        try
        {
            view.HostPosition = Position;
        }
        finally
        {
            _isSyncingHostPosition = false;
        }
    }

    private void UpdateHostWindowStateFromWindow(ViewBase view)
    {
        if (_isSyncingHostWindowState || !_isShowed)
        {
            return;
        }

        if (view.IsSet(ViewBase.HostWindowStateProperty) && view.HostWindowState == WindowState)
        {
            return;
        }

        _isSyncingHostWindowState = true;
        try
        {
            view.HostWindowState = WindowState;
        }
        finally
        {
            _isSyncingHostWindowState = false;
        }
    }

    private void ApplyHostFeatures(ViewBase view)
    {
        var features = WindowFeatures.None;
        if (view.HostFeatures != null)
        {
            foreach (var feature in view.HostFeatures)
            {
                features |= feature;
            }
        }

        SetHostFeatures(_appliedHostFeatures & ~features, false);
        SetHostFeatures(features & ~_appliedHostFeatures, true);
        _appliedHostFeatures = features;
    }

    private void ApplyWindowFeatures(ViewBase view)
    {
        if (IsMobileMode)
        {
            return;
        }

        SetCurrentValue(Window.SizeToContentProperty, view.SizeToContent);
        SetCurrentValue(Window.CanResizeProperty, view.CanResize);
        SetCurrentValue(Window.CanMaximizeProperty, view.CanMaximize);
        SetCurrentValue(Window.MinWidthProperty, view.MinWidth);
        SetCurrentValue(Window.MinHeightProperty, view.MinHeight);
        SetCurrentValue(Window.MaxWidthProperty, view.MaxWidth);
        SetCurrentValue(Window.MaxHeightProperty, view.MaxHeight);
        ApplyViewFeatures(view);
    }

    private void SetHostFeatures(WindowFeatures features, bool state)
    {
        if (features == WindowFeatures.None)
        {
            return;
        }

        if ((features & WindowFeatures.Topmost) != 0)
        {
            Topmost = state;
        }
        PlatformServices.WindowPlatformService.SetWindowFeature(this, features, state);
    }

    private void CurrentViewHostFeaturesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_currentView != null && ReferenceEquals(sender, _observedHostFeatures))
        {
            ApplyHostFeatures(_currentView);
        }
    }

    private void UpdateHostFeaturesObserver(ViewBase view)
    {
        if (!ReferenceEquals(_currentView, view))
        {
            return;
        }

        if (_observedHostFeatures != null)
        {
            _observedHostFeatures.CollectionChanged -= CurrentViewHostFeaturesOnCollectionChanged;
        }

        _observedHostFeatures = view.HostFeatures;
        if (_observedHostFeatures != null)
        {
            _observedHostFeatures.CollectionChanged += CurrentViewHostFeaturesOnCollectionChanged;
        }

        ApplyHostFeatures(view);
    }

    private void SetCurrentView(ViewBase? view)
    {
        if (ReferenceEquals(_currentView, view))
        {
            return;
        }

        if (_currentView != null)
        {
            _currentView.Loaded -= CurrentView_OnLoaded;
            _currentViewHostPositionObserver?.Dispose();
            _currentViewHostPositionObserver = null;
            _currentViewHostWindowStateObserver?.Dispose();
            _currentViewHostWindowStateObserver = null;
            _currentViewUseInlineHeaderObserver?.Dispose();
            _currentViewUseInlineHeaderObserver = null;
            _currentViewHostShowAsDialogObserver?.Dispose();
            _currentViewHostShowAsDialogObserver = null;
            _currentViewHeaderHeightOverrideObserver?.Dispose();
            _currentViewHeaderHeightOverrideObserver = null;
            _currentViewHostFeaturesObserver?.Dispose();
            _currentViewHostFeaturesObserver = null;
            if (_observedHostFeatures != null)
            {
                _observedHostFeatures.CollectionChanged -= CurrentViewHostFeaturesOnCollectionChanged;
                _observedHostFeatures = null;
            }
        }

        _currentView = view;

        if (_currentView == null)
        {
            return;
        }

        _currentView.Loaded += CurrentView_OnLoaded;
        _currentViewHostPositionObserver = _currentView.GetObservable(ViewBase.HostPositionProperty)
            .Subscribe(_ => ApplyHostPositionToWindow(_currentView));
        _currentViewHostWindowStateObserver = _currentView.GetObservable(ViewBase.HostWindowStateProperty)
            .Subscribe(_ => ApplyHostWindowStateToWindow(_currentView));
        _currentViewUseInlineHeaderObserver = _currentView.GetObservable(ViewBase.UseInlineHeaderProperty)
            .Subscribe(_ => ApplyViewFeatures(_currentView));
        _currentViewHostShowAsDialogObserver = _currentView.GetObservable(ViewBase.ShowAsDialogProperty)
            .Subscribe(_ => ApplyViewFeatures(_currentView));
        _currentViewHeaderHeightOverrideObserver = _currentView.GetObservable(NavigationPage.BarHeightOverrideProperty)
            .Subscribe(_ => ApplyViewFeatures(_currentView));
        _currentViewHostFeaturesObserver = _currentView.GetObservable(ViewBase.HostFeaturesProperty)
            .Subscribe(_ => UpdateHostFeaturesObserver(_currentView));
        ApplyHostBoundsToWindow(_currentView);
        ApplyWindowFeatures(_currentView);
    }

    private void CurrentView_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is ViewBase view && ReferenceEquals(NavigationPage.CurrentPage, view))
        {
            ApplyHostBoundsToWindow(view);
        }
    }

    private void PreShow(ViewBase? view = null)
    {
        if (IsMobileMode)
        {
            Width = 360;
            Height = 800;
            PseudoClasses.Set(":mobile", true);
            return;
        }

        if (view != null)
        {
            WindowStartupLocation = view.HostStartupLocation;
            Title = view.Header?.ToString() ?? "ClassIsland";
            ApplyHostBoundsToWindow(view);
            ApplyWindowFeatures(view);
        }
    }

    public override void Show()
    {
        PreShow();
        base.Show();
        _isShowed = true;
    }

    public void Show(IViewHost owner)
    {
        PreShow();
        Show(owner, false);
    }

    private void Show(IViewHost? owner, bool modal)
    {
        if (owner is WindowViewHost host)
        {
            PreShow();
            MyOwner = host;
            if (modal)
            {
                _ = ShowDialog(host);
            }
            else
            {
                base.Show(host);
            }
            _isShowed = true;
        }
        else
        {
            Show();
        }
        
    }

    private void ShowNativeModal(Window owner)
    {
        PreShow();
        _ = ShowDialog(owner);
        _isShowed = true;
    }

    private async Task ShowViewCore(ViewBase view, ViewBase? owner, Window? windowOwner, bool modal)
    {
        if (!ActivatedViewSet.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活到此视图宿主才能显示。");
        }

        if (owner is { AssociatedViewHost: null })
        {
            throw new InvalidOperationException("视图所有者必须已经激活到此视图宿主才能显示。");
        }
        
        if (!_isShowed)
        {
            PreShow(view);
            if (windowOwner != null)
            {
                ShowNativeModal(windowOwner);
            }
            else
            {
                Show(owner?.AssociatedViewHost, modal);
            }
            ApplyHostFeatures(view);
        }
        
        Activate();
        if (_hasLoadedInitialContent)
        {
            await NavigationPage.PushAsync(view);
        }
        else
        {
#if DEBUG
            var animationWaitStartedAt = Stopwatch.GetTimestamp();
            var viewLoadStartedAt = 0L;
            var uiBlockingStartedAt = 0L;
            var animationWaitDuration = TimeSpan.Zero;
            var viewLoadDuration = TimeSpan.Zero;
            var uiBlockingDuration = TimeSpan.Zero;
#endif

            ContentLoadingProgressRing.IsVisible = true;
            try
            {
                if (!IThemeService.IsWaitForTransientDisabled)
                {
                    await WaitForWindowInitializedAsync();
                    await WaitForNextRenderedFrameAsync();
                }
#if DEBUG
                animationWaitDuration = Stopwatch.GetElapsedTime(animationWaitStartedAt);
                viewLoadStartedAt = Stopwatch.GetTimestamp();
                uiBlockingStartedAt = viewLoadStartedAt;
                await NavigationPage.PushAsync(view);
                viewLoadDuration = Stopwatch.GetElapsedTime(viewLoadStartedAt);
#else
                await NavigationPage.PushAsync(view);
#endif
                _hasLoadedInitialContent = true;
                SetCurrentView(view);
            }
            finally
            {
                ContentLoadingProgressRing.IsVisible = false;
            }

#if DEBUG
            await WaitForWindowInitializedAsync();
            await WaitForNextRenderedFrameAsync();
            uiBlockingDuration = Stopwatch.GetElapsedTime(uiBlockingStartedAt);
            ShowInitialContentLoadTimingToast(animationWaitDuration, viewLoadDuration, uiBlockingDuration);
#endif
            return;
        }
        SetCurrentView(view);
    }

#if DEBUG
    private void ShowInitialContentLoadTimingToast(TimeSpan animationWaitDuration, TimeSpan viewLoadDuration, TimeSpan uiBlockingDuration)
    {
        this.ShowToast(new ToastMessage($"(debug) \n窗口加载时动画等待 {animationWaitDuration.TotalMilliseconds:F1} ms\n视图实际加载 {viewLoadDuration.TotalMilliseconds:F1} ms\n界面阻塞 {uiBlockingDuration.TotalMilliseconds:F1} ms。")
        {
            Duration = TimeSpan.FromSeconds(10)
        });
    }
#endif

    private Task WaitForWindowInitializedAsync()
    {
        if (_isWindowLoaded)
        {
            return Task.CompletedTask;
        }

        _windowLoadedCompletionSource ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return _windowLoadedCompletionSource.Task;
    }

    private Task WaitForNextRenderedFrameAsync()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var compositor = ElementComposition.GetElementVisual(this)?.Compositor
                         ?? throw new InvalidOperationException("窗口必须连接到可视树后才能等待渲染完成。");

        // The composition update runs after binding and layout, immediately before
        // the batch is committed. Its Rendered task is the first reliable signal
        // that the frame containing the current visibility state was drawn.
        compositor.RequestCompositionUpdate(() =>
        {
            var batch = compositor.RequestCompositionBatchCommitAsync();
            _ = CompleteWhenRenderedAsync(batch.Rendered, completion);
        });
        return completion.Task;
    }

    private static async Task CompleteWhenRenderedAsync(Task renderedTask, TaskCompletionSource completion)
    {
        try
        {
            await renderedTask.ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }
    }

    public async Task ShowView(ViewBase view, ViewBase? owner = null)
    {
        await ShowViewCore(view, owner, null, false);
    }

    public async Task ShowViewModal(ViewBase view, ViewBase owner)
    {
        await ShowViewCore(view, owner, null, true);
    }

    public async Task ShowViewModal(ViewBase view, Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        await ShowViewCore(view, null, owner, true);
    }

    public async Task<bool> HideView(ViewBase view)
    {
        if (!ActivatedViewSet.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活才能隐藏。");
        }

        if (!Equals(NavigationPage.CurrentPage, view))
        {
            return false;
        }

        UpdateHostBoundsFromWindow(view);
        if (NavigationPage.Pages?.Count() <= 1)
        {
            Close();
        }
        else
        {
            await NavigationPage.PopAsync();
        }

        return !ActivatedViewSet.Contains(view);
    }

    private void NavigationPage_OnPopped(object? sender, NavigationEventArgs e)
    {
        if (_isClosed)
        {
            return;
        }
        if (e.Page is not ViewBase viewBase)
        {
            return;
        }
        if (ActivatedViewSet.Remove(viewBase))
        {
            viewBase.ViewDeactivated();
        }

        SetCurrentView(NavigationPage.CurrentPage as ViewBase);
        if (NavigationPage.CurrentPage is ViewBase currentView)
        {
            ApplyHostBoundsToWindow(currentView);
        }
    }
}
