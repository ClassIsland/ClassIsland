using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Views;

namespace ClassIsland.Controls.UI;

/// <summary>
/// iOS/iPadOS 使用的 Avalonia 移动视图宿主。
/// </summary>
/// <remarks>
/// 严格沿用 AndroidViewHost 的页面导航、输入法避让和 MVE 生命周期行为，
/// 但不要求修改 Android 项目；平台入口只负责传入关闭原生容器的回调。
/// </remarks>
public partial class MobileViewHost : UserControl, IViewHost
{
    private const double InputPaneClearance = 12;

    private readonly Action? _hide;

    public ViewBase? CurrentView => _currentView;
    IReadOnlyCollection<ViewBase> IViewHost.ActivatedViews => ActivatedViews;

    private HashSet<ViewBase> ActivatedViews { get; } = [];

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

    public MobileViewHost(Action? hide = null)
    {
        _hide = hide;
        _isShowed = true;
        this.UseMyWindowExt();
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        AppBase.Current.PhonyRootWindow = TopLevel.GetTopLevel(this)!;
        base.OnLoaded(e);

        AttachInputPane();
        UpdateFocusedTextPresenter();
        RemoveHandler(GotFocusEvent, OnDescendantGotFocus);
        AddHandler(GotFocusEvent, OnDescendantGotFocus, RoutingStrategies.Bubble, true);
        SizeChanged -= OnHostSizeChanged;
        SizeChanged += OnHostSizeChanged;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        DetachInputPane();
        DetachFocusedTextPresenter();
        RemoveHandler(GotFocusEvent, OnDescendantGotFocus);
        SizeChanged -= OnHostSizeChanged;
        ResetPageContentOffset();
        base.OnUnloaded(e);
    }

    /// <summary>
    /// 通知宿主其原生容器已经销毁。
    /// </summary>
    internal void Destroy()
    {
        DetachInputPane();
        DetachFocusedTextPresenter();
        ResetPageContentOffset();
        PreClosing(false);
        _isClosed = true;
        SetCurrentView(null);
        NavigationPage.PopAllModalsAsync(null);
        NavigationPage.PopToRootAsync(null);
        NavigationPage.ReplaceAsync(new ContentPage(), null);
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

    private bool PreClosing(bool cancelable)
    {
        var view = ActivatedViews.LastOrDefault();
        if (view == null)
        {
            return false;
        }

        if (view.ViewDeactivating(WindowCloseReason.Undefined, true, cancelable))
        {
            foreach (var view1 in ActivatedViews)
            {
                if (view1 != view)
                {
                    view1.ViewDeactivating(WindowCloseReason.Undefined, true, cancelable);
                }
                view1.ViewDeactivated();
            }
            return false;
        }

        return true;
    }

    public void Hide()
    {
        _hide?.Invoke();
    }

    public void Activate()
    {

    }

    public IViewHost? Owner { get; }

    public bool ActivateView(ViewBase view)
    {
        if (ActivatedViews.Contains(view))
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
        if (view is SplashView)
        {
            _isFirstViewShowed = true;
        }
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
        if (!ActivatedViews.Contains(view))
        {
            throw new InvalidOperationException("视图必须已经激活才能隐藏。");
        }

        if (!Equals(NavigationPage.CurrentPage, view))
        {
            return false;
        }

        if (!view.ViewDeactivating(WindowCloseReason.Undefined, true, true))
        {
            return false;
        }

        if (NavigationPage.Pages?.Count() <= 1)
        {
            Hide();
            if (ActivatedViews.Remove(view))
            {
                view.ViewDeactivated();
            }
        }
        else
        {
            await RunNavigationWithProgressAsync(() => NavigationPage.PopAsync());
        }

        return !ActivatedViews.Contains(view);
    }

    private async Task RunNavigationWithProgressAsync(Func<Task> navigation)
    {
        var animationVersion = StartNavigationProgressAnimation();
        try
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                await navigation();
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
        if (ActivatedViews.Remove(viewBase))
        {
            viewBase.ViewDeactivated();
        }

        SetCurrentView(NavigationPage.CurrentPage as ViewBase);
    }
}
