using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Controls;

public class Lazy : ContentControl
{
    private ContentPresenter? _contentPresenter;
    private ExpressiveLoadingIndicator? _loadingIndicator;
    private bool _isContentChangesPending = true;

    public Lazy()
    {
        this.GetObservable(ContentProperty).Subscribe(_ => UpdateContent());
        this.GetObservable(ContentTemplateProperty).Subscribe(_ => UpdateContent());
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isContentChangesPending)
        {
            UpdateContent();
        }
    }

    private void UpdateContent()
    {
        _isContentChangesPending = true;
        if (!IsLoaded)
        {
            return;
        }

        _isContentChangesPending = false;
        if (IThemeService.IsWaitForTransientDisabled)
        {
            _contentPresenter?.Content = Content;
            _contentPresenter?.ContentTemplate = ContentTemplate;
            SetCompositionOpacity(_contentPresenter, 1f);
            SetCompositionOpacity(_loadingIndicator, 0f);
            return;
        }
        SetCompositionOpacity(_contentPresenter, 0f);
        SetCompositionOpacity(_loadingIndicator, 1f, 100);
        _loadingIndicator?.IsActive = true;
        _loadingIndicator?.IsVisible = true;
        Dispatcher.Post(() =>
        {
            _contentPresenter?.Content = Content;
            _contentPresenter?.ContentTemplate = ContentTemplate;
            Dispatcher.Post(() =>
            {
                SetCompositionOpacity(_contentPresenter, 1f);
                SetCompositionOpacity(_loadingIndicator, 0f);
                _loadingIndicator?.IsVisible = false;
                _loadingIndicator?.IsActive = false;
            }, DispatcherPriority.Loaded);
        });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _loadingIndicator = e.NameScope.Find<ExpressiveLoadingIndicator>("PART_LoadingIndicator");

        if (_contentPresenter != null)
        {
            SetupImplicitTransitionAnimation(_contentPresenter);
        }
        if (_loadingIndicator != null)
        {
            SetupImplicitTransitionAnimation(_loadingIndicator);
        }
        
        UpdateContent();
        base.OnApplyTemplate(e);
    }

    private void SetupImplicitTransitionAnimation(Control control)
    {
        var element = ElementComposition.GetElementVisual(control);
        if (element == null)
        {
            return;
        }

        var compositor = element.Compositor;
        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.Target = "Opacity";
        opacityAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", Easing.Parse("0,0 0,1"));
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(150);
        var implicitAnimations = compositor.CreateImplicitAnimationCollection();
        implicitAnimations["Opacity"] = opacityAnimation;
        element.ImplicitAnimations = implicitAnimations;
    }

    private void SetCompositionOpacity(Control? control, float opacity, double delayMs=0)
    {
        if (control == null) return;
        var element = ElementComposition.GetElementVisual(control);
        if (element == null)
        {
            return;
        }

        var compositor = element.Compositor;
        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.Target = "Opacity";
        opacityAnimation.InsertKeyFrame(1.0f, opacity, Easing.Parse("0,0 0,1"));
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);
        opacityAnimation.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        element.StartAnimation("Opacity", opacityAnimation);
    }
}