using System.Runtime.Versioning;
using Android.Content;
using Avalonia;
using Avalonia.Android;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ClassIsland.Controls.UI;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Views;

namespace ClassIsland.Android.Controls.UI;

[SupportedOSPlatform("android24.0")]
public partial class AndroidViewHost : UserControl, IViewHost
{
    private const double InputPaneClearance = 12;

    public MainActivity Activity { get; }

    public ViewBase? CurrentView => _currentView;
    IReadOnlyCollection<ViewBase> IViewHost.ActivatedViews => ActivatedViews;

    private HashSet<ViewBase> ActivatedViews { get; } = [];

    private HashSet<ViewBase> DrawerActivatedViews { get; } = [];

    IReadOnlyCollection<ViewBase> IViewHost.DrawerActivatedViews => DrawerActivatedViews;

    public bool IsDrawerOpen => DrawerHost.IsOpen;

    public ViewBase? CurrentDrawerView => DrawerHost.CurrentView;

    private IInputElement? _drawerPreviousFocus;

    private bool _isDrawerNavigationBusy;

    private bool _isShowed = false;

    private bool _isFirstViewShowed = false;

    private bool _isClosed = false;

    private bool _isSyncingHostSize = false;

    private bool _isSyncingHostPosition = false;

    private bool _isSyncingHostWindowState = false;

    private ViewBase? _currentView;

    private IDisposable? _currentViewHostPositionObserver;

    private IDisposable? _currentViewHostWindowStateObserver;

    private IDisposable? _currentViewUseInlineHeaderObserver;
    
    private IDisposable? _currentViewHostShowAsDialogObserver;
    
    private IDisposable? _currentViewHeaderHeightOverrideObserver;

    private int _navigationProgressAnimationVersion;

    private IInputPane? _inputPane;

    private TimeSpan _inputPaneAnimationDuration;

    private IEasing? _inputPaneAnimationEasing;

    private CancellationTokenSource? _inputPaneAnimationCancellation;

    private bool _isInputPaneOffsetUpdatePending;

    private TextPresenter? _focusedTextPresenter;

    private Task? _releaseTask;

    public AndroidViewHost(MainActivity activity)
    {
        Activity = activity;
        Activity.Destroy += OnDestroy;
        _isShowed = true;
        this.UseMyWindowExt();
        InitializeComponent();
        DrawerHost.ClosingRequested += DrawerHostOnClosingRequested;
        DrawerHost.Closed += DrawerHostOnClosed;
        DrawerHost.Popped += DrawerHostOnPopped;
        DrawerHost.BackRequested += DrawerHostOnBackRequested;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (_isClosed)
        {
            return;
        }
        AppBase.Current.PhonyRootWindow = TopLevel.GetTopLevel(this)!;
        Console.WriteLine($"[ELYSIADBG] Get TopLevel = {TopLevel.GetTopLevel(this)}");
        base.OnLoaded(e);

        AppBase.Current.AppStarted -= BackgroundOnAppStarted;
        AppBase.Current.AppStarted += BackgroundOnAppStarted;
        BindBackgroundSettings();
        AttachInputPane();
        UpdateFocusedTextPresenter();
        RemoveHandler(GotFocusEvent, OnDescendantGotFocus);
        AddHandler(GotFocusEvent, OnDescendantGotFocus, RoutingStrategies.Bubble, true);
        SizeChanged -= OnHostSizeChanged;
        SizeChanged += OnHostSizeChanged;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        AppBase.Current.AppStarted -= BackgroundOnAppStarted;
        BackgroundLayer.DataContext = null;
        DetachInputPane();
        DetachFocusedTextPresenter();
        RemoveHandler(GotFocusEvent, OnDescendantGotFocus);
        SizeChanged -= OnHostSizeChanged;
        ResetPageContentOffset();
        base.OnUnloaded(e);
    }

    private void BackgroundOnAppStarted(object? sender, EventArgs e) => BindBackgroundSettings();

    private void BindBackgroundSettings() =>
        BackgroundLayer.DataContext = IAppHost.TryGetService<SettingsService>();

    private async void OnDestroy(object? sender, EventArgs e)
    {
        try
        {
            await ReleaseHostAsync();
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Warn(nameof(AndroidViewHost), exception.ToString());
        }
    }

    internal Task ReleaseHostAsync() => _releaseTask ??= ReleaseHostCoreAsync();

    private async Task ReleaseHostCoreAsync()
    {
        _isClosed = true;
        IsEnabled = false;
        Activity.Destroy -= OnDestroy;
        AppBase.Current.AppStarted -= BackgroundOnAppStarted;
        BackgroundLayer.DataContext = null;
        DetachInputPane();
        DetachFocusedTextPresenter();
        RemoveHandler(GotFocusEvent, OnDescendantGotFocus);
        SizeChanged -= OnHostSizeChanged;
        ResetPageContentOffset();
        ++_navigationProgressAnimationVersion;
        var visual = ElementComposition.GetElementVisual(NavigationProgressBarFill);
        visual?.StopAnimation(nameof(visual.Scale));
        visual?.StopAnimation(nameof(visual.Opacity));
        SetCurrentView(null);
        _drawerPreviousFocus = null;
        DrawerHost.ClosingRequested -= DrawerHostOnClosingRequested;
        DrawerHost.Closed -= DrawerHostOnClosed;
        DrawerHost.Popped -= DrawerHostOnPopped;
        DrawerHost.BackRequested -= DrawerHostOnBackRequested;

        foreach (var view in ActivatedViews.Concat(DrawerActivatedViews).Distinct().ToArray())
        {
            view.ViewDeactivating(WindowCloseReason.Undefined, true, false);
            view.ViewDeactivated();
        }
        ActivatedViews.Clear();
        DrawerActivatedViews.Clear();

        // NavigationPage ignores stack changes during transitions. Let an in-flight transition finish
        // before removing pages so it cannot retain the old host's views after a reload.
        while (!Activity.IsDestroyed && (NavigationPage.IsNavigating || DrawerHost.NavigationPage.IsNavigating))
        {
            await Task.Delay(16);
        }
        DrawerHost.Clear();
        await NavigationPage.PopAllModalsAsync(null);
        foreach (var page in NavigationPage.NavigationStack.ToArray())
        {
            NavigationPage.RemovePage(page);
        }
    }

    public bool HandleBackRequested()
    {
        if (IsDrawerOpen)
        {
            _ = NavigateDrawerBack();
            return true;
        }

        var currentView = _currentView ?? ActivatedViews.LastOrDefault();
        if (currentView != null && NavigationPage.Pages?.Count() > 1)
        {
            _ = HideView(currentView);
            return true;
        }

        return false;
    }

    private void AttachInputPane()
    {
        DetachInputPane();
        _inputPane = TopLevel.GetTopLevel(this)?.InputPane;
        if (_inputPane == null)
        {
            return;
        }

        _inputPane.StateChanged += InputPane_OnStateChanged;
    }

    private void DetachInputPane()
    {
        if (_inputPane != null)
        {
            _inputPane.StateChanged -= InputPane_OnStateChanged;
            _inputPane = null;
        }

        CancelInputPaneAnimation();
    }

    private void InputPane_OnStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (!ReferenceEquals(sender, _inputPane))
        {
            return;
        }

        UpdateFocusedTextPresenter();
        _isInputPaneOffsetUpdatePending = false;
        if (e.NewState == InputPaneState.Open)
        {
            _inputPaneAnimationDuration = e.AnimationDuration;
            _inputPaneAnimationEasing = e.Easing;
        }

        var targetOffset = e.NewState == InputPaneState.Open
            ? CalculatePageContentOffset(e.EndRect)
            : 0;
        _ = AnimatePageContentOffsetAsync(targetOffset, e.AnimationDuration, e.Easing);
    }

    private void OnDescendantGotFocus(object? sender, FocusChangedEventArgs e)
    {
        UpdateFocusedTextPresenter();
        UpdatePageContentOffsetForOpenInputPane();
    }

    private void UpdateFocusedTextPresenter()
    {
        var focusedElement = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        var textBox = focusedElement as TextBox ??
                      focusedElement?.GetVisualAncestors().OfType<TextBox>().FirstOrDefault();
        var textPresenter = textBox?.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        if (ReferenceEquals(_focusedTextPresenter, textPresenter))
        {
            return;
        }

        DetachFocusedTextPresenter();
        _focusedTextPresenter = textPresenter;
        if (_focusedTextPresenter != null)
        {
            _focusedTextPresenter.CaretBoundsChanged += FocusedTextPresenter_OnCaretBoundsChanged;
        }
    }

    private void DetachFocusedTextPresenter()
    {
        if (_focusedTextPresenter != null)
        {
            _focusedTextPresenter.CaretBoundsChanged -= FocusedTextPresenter_OnCaretBoundsChanged;
            _focusedTextPresenter = null;
        }
    }

    private void FocusedTextPresenter_OnCaretBoundsChanged(object? sender, EventArgs e)
    {
        UpdatePageContentOffsetForOpenInputPane();
    }

    private void OnHostSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdatePageContentOffsetForOpenInputPane();
    }

    private void UpdatePageContentOffsetForOpenInputPane()
    {
        if (_inputPane is not { State: InputPaneState.Open } || _inputPaneAnimationEasing == null)
        {
            return;
        }

        if (_inputPaneAnimationCancellation != null)
        {
            _isInputPaneOffsetUpdatePending = true;
            return;
        }

        var targetOffset = CalculatePageContentOffset(_inputPane.OccludedRect);
        _ = AnimatePageContentOffsetAsync(
            targetOffset,
            _inputPaneAnimationDuration,
            _inputPaneAnimationEasing);
    }

    private double CalculatePageContentOffset(Rect occludedRect)
    {
        if (occludedRect.Width <= 0 || occludedRect.Height <= 0)
        {
            return 0;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.FocusManager?.GetFocusedElement() is not Visual focusedElement ||
            !PageContentRoot.IsVisualAncestorOf(focusedElement))
        {
            return 0;
        }

        var focusedTextBox = focusedElement as TextBox ??
                             focusedElement.GetVisualAncestors().OfType<TextBox>().FirstOrDefault();
        var avoidanceElement = focusedTextBox ?? focusedElement;
        var focusedTopInContent = avoidanceElement.TranslatePoint(default, PageContentRoot);
        var focusedBottomInContent = avoidanceElement.TranslatePoint(
            new Point(0, avoidanceElement.Bounds.Height), PageContentRoot);
        var contentTopInTopLevel = PageContentRoot.TranslatePoint(default, topLevel);
        if (focusedTopInContent == null || focusedBottomInContent == null || contentTopInTopLevel == null)
        {
            return 0;
        }

        var currentOffset = PageContentRoot.RenderTransform is TranslateTransform transform
            ? transform.Y
            : 0;
        var unshiftedContentTop = contentTopInTopLevel.Value.Y - currentOffset;
        var unshiftedFocusedTop = unshiftedContentTop + focusedTopInContent.Value.Y;
        var unshiftedFocusedBottom = unshiftedContentTop + focusedBottomInContent.Value.Y;
        var targetOffset = Math.Min(
            0,
            occludedRect.Top - InputPaneClearance - unshiftedFocusedBottom);

        if (unshiftedFocusedTop + targetOffset >= 0 ||
            focusedTextBox == null ||
            !TryGetCaretBottomInContent(focusedTextBox, out var caretBottomInContent))
        {
            return targetOffset;
        }

        var unshiftedCaretBottom = unshiftedContentTop + caretBottomInContent;
        return Math.Min(0, occludedRect.Top - InputPaneClearance - unshiftedCaretBottom);
    }

    private bool TryGetCaretBottomInContent(TextBox textBox, out double caretBottom)
    {
        caretBottom = 0;
        var presenter = _focusedTextPresenter != null && textBox.IsVisualAncestorOf(_focusedTextPresenter)
            ? _focusedTextPresenter
            : textBox.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        if (presenter?.TextLayout == null)
        {
            return false;
        }

        var preeditText = presenter.PreeditText;
        var preeditCursorPosition = presenter.PreeditTextCursorPosition is >= 0 and var cursorPosition &&
                                    cursorPosition <= preeditText?.Length
            ? cursorPosition
            : preeditText?.Length ?? 0;
        var textLength = (presenter.Text?.Length ?? 0) + (preeditText?.Length ?? 0);
        var caretIndex = Math.Clamp(presenter.CaretIndex + preeditCursorPosition, 0, textLength);
        var caretRect = presenter.TextLayout.HitTestTextPosition(caretIndex);
        var caretBottomInContent = presenter.TranslatePoint(
            new Point(caretRect.X, caretRect.Bottom),
            PageContentRoot);
        if (caretBottomInContent == null)
        {
            return false;
        }

        caretBottom = caretBottomInContent.Value.Y;
        return true;
    }

    private async Task AnimatePageContentOffsetAsync(
        double targetOffset,
        TimeSpan duration,
        IEasing easing)
    {
        if (PageContentRoot.RenderTransform is not TranslateTransform transform)
        {
            return;
        }

        var startOffset = transform.Y;
        CancelInputPaneAnimation();

        if (duration <= TimeSpan.Zero || Math.Abs(startOffset - targetOffset) < 0.01)
        {
            transform.Y = targetOffset;
            return;
        }

        var cancellation = new CancellationTokenSource();
        _inputPaneAnimationCancellation = cancellation;
        var animation = new Animation
        {
            Duration = duration,
            Easing = new InputPaneAnimationEasing(easing),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(TranslateTransform.YProperty, startOffset)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(TranslateTransform.YProperty, targetOffset)
                    }
                }
            }
        };

        try
        {
            await animation.RunAsync(transform, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            var updatePendingOffset = false;
            if (ReferenceEquals(_inputPaneAnimationCancellation, cancellation))
            {
                _inputPaneAnimationCancellation = null;
                if (!cancellation.IsCancellationRequested)
                {
                    transform.Y = targetOffset;
                }

                updatePendingOffset = _isInputPaneOffsetUpdatePending;
                _isInputPaneOffsetUpdatePending = false;
            }

            cancellation.Dispose();
            if (updatePendingOffset)
            {
                UpdatePageContentOffsetForOpenInputPane();
            }
        }
    }

    private void CancelInputPaneAnimation()
    {
        _inputPaneAnimationCancellation?.Cancel();
        _inputPaneAnimationCancellation = null;
    }

    private void ResetPageContentOffset()
    {
        _isInputPaneOffsetUpdatePending = false;
        CancelInputPaneAnimation();
        if (PageContentRoot.RenderTransform is TranslateTransform transform)
        {
            transform.Y = 0;
        }
    }

    private sealed class InputPaneAnimationEasing(IEasing easing) : Easing
    {
        public override double Ease(double progress) => easing.Ease(progress);
    }

    public void Hide()
    {
        Activity.Finish();
    }

    public new void Activate()
    {
        
    }

    public IViewHost? Owner { get; }

    public bool ActivateView(ViewBase view)
    {
        if (_isClosed || ActivatedViews.Contains(view))
        {
            return false;
        }

        if (!view.ViewActivating(this))
        {
            return false;
        }
        ActivatedViews.Add(view);
        view.ViewActivated(this);

        return true;
    }

    public bool DeactivateView(ViewBase view)
    {
        if (!ActivatedViews.Contains(view))
        {
            return false;
        }

        if (!view.ViewDeactivating(WindowCloseReason.Undefined, true, true))
        {
            return false;
        }
        ActivatedViews.Remove(view);
        view.ViewDeactivated();
        
        return true;
    }
    

    private void ApplyViewFeatures(ViewBase view)
    {
        
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
        }

        _currentView = view;

        if (_currentView == null)
        {
            return;
        }

        _currentView.Loaded += CurrentView_OnLoaded;
        _currentViewUseInlineHeaderObserver = _currentView.GetObservable(ViewBase.UseInlineHeaderProperty)
            .Subscribe(_ => ApplyViewFeatures(_currentView));
        _currentViewHostShowAsDialogObserver = _currentView.GetObservable(ViewBase.ShowAsDialogProperty)
            .Subscribe(_ => ApplyViewFeatures(_currentView));
        _currentViewHeaderHeightOverrideObserver = _currentView.GetObservable(NavigationPage.BarHeightOverrideProperty)
            .Subscribe(_ => ApplyViewFeatures(_currentView));
        ApplyViewFeatures(_currentView);
    }

    private void CurrentView_OnLoaded(object? sender, RoutedEventArgs e)
    {
    }

    private void PreShow()
    {
    }

    public void Show()
    {
        
    }

    public void Show(IViewHost owner)
    {
        PreShow();
        Show(owner, false);
    }

    private void Show(IViewHost? owner, bool modal)
    {
        
    }

    private async Task ShowViewCore(ViewBase view, ViewBase? owner, bool modal)
    {
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活到此视图宿主才能显示。");
        }

        if (owner is { AssociatedViewHost: null })
        {
            throw new InvalidOperationException("视图所有者必须已经激活到此视图宿主才能显示。");
        }
        
        if (!_isShowed)
        {
            Show(owner?.AssociatedViewHost, modal);
        }
        
        Activate();
        var isFirstViewShowed = _isFirstViewShowed;
        await RunNavigationWithProgressAsync(async () =>
        {
            if (isFirstViewShowed)
            {
                await NavigationPage.PushAsync(view);
            }
            else
            {
                await NavigationPage.ReplaceAsync(view);
            }
        });
        if (_isClosed)
        {
            return;
        }
        _isFirstViewShowed = true;
        SetCurrentView(view);
    }

    public async Task ShowView(ViewBase view, ViewBase? owner = null)
    {
        await ShowViewCore(view, owner, false);
    }

    public async Task ShowViewModal(ViewBase view, ViewBase owner)
    {
        await ShowViewCore(view, owner, true);
    }

    public async Task ShowViewModal(ViewBase view, Window owner)
    {
        await ShowViewCore(view, null, true);
    }

    public async Task<bool> HideView(ViewBase view)
    {
        if (DrawerActivatedViews.Contains(view))
        {
            if (!Equals(CurrentDrawerView, view))
            {
                return false;
            }
            if (DrawerHost.CanGoBack)
            {
                await DrawerHost.PopAsync();
            }
            else
            {
                await HideDrawer();
            }
            return !DrawerActivatedViews.Contains(view);
        }

        Console.WriteLine($"[ELYSIADBG] Going to hide view {view}");
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活才能隐藏。");
        }

        if (!Equals(NavigationPage.CurrentPage, view))
        {
            return false;
        }
        
        if (!DeactivateView(view))
        {
            return false;
        }

        if (NavigationPage.Pages?.Count() <= 1)
        {
            Hide();
        }
        else
        {
            await RunNavigationWithProgressAsync(() => NavigationPage.PopAsync());
        }

        return true;
    }

    public async Task ShowDrawer(ViewBase view, DrawerOptions? options = null)
    {
        if (IsDrawerOpen)
        {
            throw new InvalidOperationException("当前视图宿主已有打开的抽屉。");
        }
        if (view.AssociatedViewHost != null)
        {
            throw new InvalidOperationException("视图已被激活，不能再次打开。");
        }

        ActivateDrawerView(view);
        _drawerPreviousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        await DrawerHost.OpenAsync(view, options);
        DrawerHost.NavigationPage.Focus();
    }

    public async Task PushDrawerView(ViewBase view)
    {
        if (!IsDrawerOpen)
        {
            throw new InvalidOperationException("当前视图宿主没有打开的抽屉。");
        }
        if (view.AssociatedViewHost != null)
        {
            throw new InvalidOperationException("视图已被激活，不能再次推入抽屉。");
        }

        ActivateDrawerView(view);
        await DrawerHost.PushAsync(view);
    }

    public async Task<bool> NavigateDrawerBack()
    {
        if (!IsDrawerOpen)
        {
            return false;
        }
        if (DrawerHost.CanGoBack)
        {
            return await DrawerHost.PopAsync();
        }
        return await HideDrawer();
    }

    public Task<bool> HideDrawer()
    {
        if (!IsDrawerOpen)
        {
            return Task.FromResult(false);
        }
        DrawerHost.RequestClose();
        return Task.FromResult(!DrawerHost.IsOpen);
    }

    private bool ActivateDrawerView(ViewBase view)
    {
        if (_isClosed || DrawerActivatedViews.Contains(view) || !view.ViewActivating(this))
        {
            return false;
        }
        DrawerActivatedViews.Add(view);
        view.ViewActivated(this);
        return true;
    }

    private async void DrawerHostOnBackRequested(object? sender, EventArgs e)
    {
        if (_isDrawerNavigationBusy)
        {
            return;
        }
        _isDrawerNavigationBusy = true;
        try
        {
            await NavigateDrawerBack();
        }
        finally
        {
            _isDrawerNavigationBusy = false;
        }
    }

    private void DrawerHostOnClosingRequested(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var view in DrawerHost.NavigationPage.NavigationStack.OfType<ViewBase>().Reverse())
        {
            if (!view.ViewDeactivating(WindowCloseReason.Undefined, true, true))
            {
                e.Cancel = true;
                return;
            }
        }
    }

    private void DrawerHostOnPopped(object? sender, NavigationEventArgs e)
    {
        if (e.Page is ViewBase view && DrawerActivatedViews.Remove(view))
        {
            view.ViewDeactivated();
        }
    }

    private void DrawerHostOnClosed(object? sender, EventArgs e)
    {
        DrawerHost.Clear();
        foreach (var view in DrawerActivatedViews.ToArray())
        {
            view.ViewDeactivated();
        }
        DrawerActivatedViews.Clear();
        if (_drawerPreviousFocus is Control control && control.IsAttachedToVisualTree())
        {
            control.Focus();
        }
        _drawerPreviousFocus = null;
    }

    private async Task RunNavigationWithProgressAsync(Func<Task> navigation)
    {
        var animationVersion = StartNavigationProgressAnimation();
        try
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if (!_isClosed)
                {
                    await navigation();
                }
            });
        }
        finally
        {
            await CompleteNavigationProgressAnimationAsync(animationVersion);
        }
    }

    private int StartNavigationProgressAnimation()
    {
        var animationVersion = ++_navigationProgressAnimationVersion;
        var visual = ElementComposition.GetElementVisual(NavigationProgressBarFill);
        if (visual == null)
        {
            return animationVersion;
        }

        var compositor = visual.Compositor;
        var progressAnimation = compositor.CreateVector3DKeyFrameAnimation();
        progressAnimation.InsertKeyFrame(0.0f, visual.Scale with { X = 0.05 });
        progressAnimation.InsertKeyFrame(0.35f, visual.Scale with { X = 0.65 }, new CubicEaseOut());
        progressAnimation.InsertKeyFrame(1.0f, visual.Scale with { X = 0.9 }, new CubicEaseOut());
        progressAnimation.Duration = TimeSpan.FromSeconds(1.5);
        visual.StartAnimation(nameof(visual.Scale), progressAnimation);

        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.InsertKeyFrame(0.0f, 1);
        opacityAnimation.InsertKeyFrame(1.0f, 1);
        opacityAnimation.Duration = progressAnimation.Duration;
        visual.StartAnimation(nameof(visual.Opacity), opacityAnimation);

        return animationVersion;
    }

    private async Task CompleteNavigationProgressAnimationAsync(int animationVersion)
    {
        if (animationVersion != _navigationProgressAnimationVersion)
        {
            return;
        }

        var visual = ElementComposition.GetElementVisual(NavigationProgressBarFill);
        if (visual == null)
        {
            return;
        }

        var completionDuration = TimeSpan.FromMilliseconds(150);
        var completionAnimation = visual.Compositor.CreateVector3DKeyFrameAnimation();
        completionAnimation.InsertKeyFrame(1.0f, visual.Scale with { X = 1 }, new CubicEaseOut());
        completionAnimation.Duration = completionDuration;
        visual.StartAnimation(nameof(visual.Scale), completionAnimation);
        await Task.Delay(completionDuration);

        if (animationVersion != _navigationProgressAnimationVersion)
        {
            return;
        }

        var fadeDuration = TimeSpan.FromMilliseconds(120);
        var fadeAnimation = visual.Compositor.CreateScalarKeyFrameAnimation();
        fadeAnimation.InsertKeyFrame(1.0f, 0, new CubicEaseOut());
        fadeAnimation.Duration = fadeDuration;
        visual.StartAnimation(nameof(visual.Opacity), fadeAnimation);
        await Task.Delay(fadeDuration);

        if (animationVersion == _navigationProgressAnimationVersion)
        {
            visual.Opacity = 0;
            visual.Scale = visual.Scale with { X = 0.05 };
        }
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
        viewBase.ViewDeactivating(WindowCloseReason.Undefined, true, true);
        viewBase.ViewDeactivated();
        ActivatedViews.Remove(viewBase);

        SetCurrentView(NavigationPage.CurrentPage as ViewBase);
    }
}
