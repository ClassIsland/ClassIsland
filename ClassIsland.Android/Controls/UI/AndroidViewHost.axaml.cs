using System.Runtime.Versioning;
using Android.Content;
using Avalonia;
using Avalonia.Android;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using ClassIsland.Controls.UI;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Views;

namespace ClassIsland.Android.Controls.UI;

[SupportedOSPlatform("android24.0")]
public partial class AndroidViewHost : UserControl, IViewHost
{
    public MainActivity Activity { get; }

    public ViewBase? CurrentView { get; }
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
    
    public AndroidViewHost(MainActivity activity)
    {
        Activity = activity;
        Activity.Destroy += OnDestroy;
        _isShowed = true;
        InitializeComponent();
    }

    private void OnDestroy(object? sender, EventArgs e)
    {
        PreClosing(false);
        _isClosed = true;
        SetCurrentView(null);
        NavigationPage.PopAllModalsAsync(null);
        NavigationPage.PopToRootAsync(null);
        NavigationPage.ReplaceAsync(new ContentPage(), null);
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
                    view.ViewDeactivating(WindowCloseReason.Undefined, true, cancelable);
                }
                view1.ViewDeactivated();
            }
            return false;
        }

        return true;
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
        viewBase.ViewDeactivating(WindowCloseReason.Undefined, true, true);
        viewBase.ViewDeactivated();
        ActivatedViews.Remove(viewBase);

        SetCurrentView(NavigationPage.CurrentPage as ViewBase);
    }
}
