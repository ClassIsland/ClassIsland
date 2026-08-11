using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

[PseudoClasses(":drawer-left", ":drawer-right", ":open", ":composition-ready")]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_IgnoreLayer", typeof(Border))]
[TemplatePart("PART_DrawerContentBorder", typeof(Border))]
public class DrawerHost : ContentControl
{
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan DrawerContentLoadDelay = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan DrawerLoadingIndicatorDelay = TimeSpan.FromMilliseconds(100);
    private static readonly Easing AnimationEasing = Easing.Parse("0,0 0,1");

    public static readonly StyledProperty<object?> DrawerContentProperty = AvaloniaProperty.Register<DrawerHost, object?>(
        nameof(DrawerContent));

    public object? DrawerContent
    {
        get => GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> DrawerContentTemplateProperty = AvaloniaProperty.Register<DrawerHost, IDataTemplate?>(
        nameof(DrawerContentTemplate));

    public IDataTemplate? DrawerContentTemplate
    {
        get => GetValue(DrawerContentTemplateProperty);
        set => SetValue(DrawerContentTemplateProperty, value);
    }

    public static readonly StyledProperty<bool> IsDrawerOpenProperty = AvaloniaProperty.Register<DrawerHost, bool>(
        nameof(IsDrawerOpen));

    public bool IsDrawerOpen
    {
        get => GetValue(IsDrawerOpenProperty);
        set => SetValue(IsDrawerOpenProperty, value);
    }

    public static readonly StyledProperty<DrawerPlacementEnum> DrawerPlacementProperty = AvaloniaProperty.Register<DrawerHost, DrawerPlacementEnum>(
        nameof(DrawerPlacement));

    public DrawerPlacementEnum DrawerPlacement
    {
        get => GetValue(DrawerPlacementProperty);
        set => SetValue(DrawerPlacementProperty, value);
    }

    public static readonly StyledProperty<double> ActualDrawerWidthProperty = AvaloniaProperty.Register<DrawerHost, double>(
        nameof(ActualDrawerWidth));

    public double ActualDrawerWidth
    {
        get => GetValue(ActualDrawerWidthProperty);
        set => SetValue(ActualDrawerWidthProperty, value);
    }

    private ContentPresenter? _contentPresenter;
    private ContentPresenter? _drawerContentPresenter;
    private Grid? _drawerLoadingIndicatorHost;
    private FAProgressRing? _drawerLoadingIndicator;
    private Border? _ignoreLayer;
    private Border? _drawerContentBorder;
    private CompositionVisual? _contentPresenterVisual;
    private CompositionVisual? _drawerContentPresenterVisual;
    private CompositionVisual? _drawerLoadingIndicatorVisual;
    private CompositionVisual? _ignoreLayerVisual;
    private CompositionVisual? _drawerContentVisual;
    private int _drawerOpenOperationId;
    private int _drawerContentLoadRequestId;
    private int _activeDrawerContentLoadRequestId;
    private bool _isDrawerOpeningPending;
    private bool _isDrawerWidthPlaceholderActive;
    private double _drawerContentBorderMinWidth;

    public DrawerHost()
    {
        this.GetObservable(DrawerPlacementProperty).Subscribe(_ =>
        {
            UpdateDrawerPlacement();
            if (!IsDrawerOpen)
            {
                UpdateDrawerOffset(animate: false);
            }
            if (!_isDrawerOpeningPending)
            {
                UpdateDrawerContentOffset();
            }
        });
        this.GetObservable(IsDrawerOpenProperty).Subscribe(_ =>
        {
            if (IsDrawerOpen)
            {
                ScheduleDrawerOpening();
            }
            else
            {
                CancelPendingDrawerOpening();
                UpdateDrawerOffset(animate: true);
                UpdateDrawerContentOffset();
            }
        });
        this.GetObservable(DrawerContentProperty).Subscribe(_ => OnDrawerContentChanged());
        this.GetObservable(ActualDrawerWidthProperty).Subscribe(_ =>
        {
            if (!IsDrawerOpen)
            {
                UpdateDrawerOffset(animate: false);
            }
            if (!_isDrawerOpeningPending)
            {
                UpdateDrawerContentOffset();
            }
        });
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            IsDrawerOpen = false;
        }
    }

    private void UpdateDrawerPlacement()
    {
        PseudoClasses.Set(":drawer-left", DrawerPlacement == DrawerPlacementEnum.Left);
        PseudoClasses.Set(":drawer-right", DrawerPlacement == DrawerPlacementEnum.Right);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        var previousDrawerContentPresenter = _drawerContentPresenter;
        ClearCompositionAnimations();
        ClearDrawerWidthPlaceholder();
        if (_drawerContentBorder != null) 
            _drawerContentBorder.SizeChanged -= DrawerContentBorderOnSizeChanged;
        if (_ignoreLayer != null) 
            _ignoreLayer.PointerPressed -= IgnoreLayerOnPointerPressed;

        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        var drawerContentPresenter = e.NameScope.Find<ContentPresenter>("PART_DrawerContentPresenter");
        if (previousDrawerContentPresenter != null &&
            !ReferenceEquals(previousDrawerContentPresenter, drawerContentPresenter))
        {
            previousDrawerContentPresenter.Content = null;
        }
        _drawerContentPresenter = drawerContentPresenter;
        _drawerLoadingIndicatorHost = e.NameScope.Find<Grid>("PART_DrawerLoadingIndicatorHost");
        _drawerLoadingIndicator = e.NameScope.Find<FAProgressRing>("PART_DrawerLoadingIndicator");
        _ignoreLayer = e.NameScope.Find<Border>("PART_IgnoreLayer");
        _drawerContentBorder = e.NameScope.Find<Border>("PART_DrawerContentBorder");

        UpdateDeferredDrawerWidth();
        UpdateDrawerContentPresentation();

        if (_drawerContentBorder != null) 
            _drawerContentBorder.SizeChanged += DrawerContentBorderOnSizeChanged;
        if (_ignoreLayer != null) 
            _ignoreLayer.PointerPressed += IgnoreLayerOnPointerPressed;

        base.OnApplyTemplate(e);
        if (IsLoaded)
        {
            ScheduleCompositionAnimationSetup();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Loaded -= OnDrawerHostLoaded;
        Loaded += OnDrawerHostLoaded;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Loaded -= OnDrawerHostLoaded;
        LayoutUpdated -= OnInitialLayoutUpdated;
        CancelPendingDrawerOpening();
        if (_drawerContentPresenter?.Content == null)
        {
            ApplyDrawerWidthPlaceholder(ActualDrawerWidth);
        }
        ClearCompositionAnimations();
        if (_drawerLoadingIndicator != null)
        {
            _drawerLoadingIndicator.IsActive = false;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void OnDrawerHostLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnDrawerHostLoaded;
        if (_drawerLoadingIndicator != null)
        {
            _drawerLoadingIndicator.IsActive = true;
        }
        SetupCompositionAnimations();
    }

    private void ScheduleCompositionAnimationSetup()
    {
        LayoutUpdated -= OnInitialLayoutUpdated;
        LayoutUpdated += OnInitialLayoutUpdated;
    }

    private void OnInitialLayoutUpdated(object? sender, EventArgs e)
    {
        LayoutUpdated -= OnInitialLayoutUpdated;
        SetupCompositionAnimations();
    }

    private void IgnoreLayerOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        IsDrawerOpen = false;
    }

    private void DrawerContentBorderOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 0)
        {
            ActualDrawerWidth = e.NewSize.Width;
        }
    }

    private void OnDrawerContentChanged()
    {
        if (_drawerContentPresenter == null)
        {
            UpdateDeferredDrawerWidth();
            return;
        }

        if (!IsDrawerOpen)
        {
            UpdateDeferredDrawerWidth();
            return;
        }

        UpdateDeferredDrawerWidth();
        if (IsDrawerContentCurrent())
        {
            if (!_isDrawerOpeningPending)
            {
                _drawerContentPresenter.IsVisible = true;
            }
            return;
        }

        _drawerContentPresenter.IsVisible = false;
        ClearStaleDrawerContent();
        UpdateDeferredDrawerWidth();
        if (DrawerContent == null)
        {
            if (!_isDrawerOpeningPending)
            {
                ++_drawerContentLoadRequestId;
            }
            CompleteDrawerContentLoadingIndicator();
            RestoreDrawerContentOpacity();
            ClearDrawerWidthPlaceholder();
        }
        else if (_isDrawerOpeningPending)
        {
            CompleteDrawerContentLoadingIndicator();
            StartDrawerContentLoadingIndicator(_drawerContentLoadRequestId);
            ApplyDrawerWidthPlaceholder(ActualDrawerWidth);
        }
        else
        {
            ScheduleDrawerContentLoad(_drawerOpenOperationId);
        }
    }

    private void ScheduleDrawerOpening()
    {
        var operationId = ++_drawerOpenOperationId;
        var loadRequestId = ++_drawerContentLoadRequestId;
        _isDrawerOpeningPending = true;
        CompleteDrawerContentLoadingIndicator();
        StartDrawerContentLoadingIndicator(loadRequestId);
        if (_drawerContentPresenter != null)
        {
            _drawerContentPresenter.IsVisible = false;
        }
        if (!IsDrawerContentCurrent())
        {
            ClearStaleDrawerContent();
        }
        UpdateDeferredDrawerWidth();
        UpdateDrawerOffset(animate: true);
        UpdateDrawerContentOffset();
        Dispatcher.UIThread.Post(
            () => BeginDrawerOpening(operationId, loadRequestId),
            DispatcherPriority.Render);
    }

    private void BeginDrawerOpening(int operationId, int loadRequestId)
    {
        if (operationId != _drawerOpenOperationId ||
            loadRequestId != _drawerContentLoadRequestId ||
            !IsDrawerOpen)
        {
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            return;
        }

        _isDrawerOpeningPending = false;
        if (_drawerContentVisual == null)
        {
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            return;
        }

        UpdateDrawerOffset(animate: true);
        UpdateDrawerContentOffset();
        if (IsDrawerContentCurrent())
        {
            if (_drawerContentPresenter != null)
            {
                _drawerContentPresenter.IsVisible = true;
            }
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            RestoreDrawerContentOpacity();
            ClearDrawerWidthPlaceholder();
        }
        else if (DrawerContent == null)
        {
            if (_drawerContentPresenter != null)
            {
                _drawerContentPresenter.IsVisible = true;
            }
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            RestoreDrawerContentOpacity();
            ClearDrawerWidthPlaceholder();
        }
        else
        {
            ClearStaleDrawerContent();
            UpdateDeferredDrawerWidth();
            ScheduleDrawerContentLoad(operationId, loadRequestId);
        }
    }

    private void ScheduleDrawerContentLoad(int operationId)
    {
        var loadRequestId = ++_drawerContentLoadRequestId;
        ScheduleDrawerContentLoad(operationId, loadRequestId);
    }

    private void ScheduleDrawerContentLoad(int operationId, int loadRequestId)
    {
        if (DrawerContent == null)
        {
            if (_drawerContentPresenter != null)
            {
                _drawerContentPresenter.IsVisible = true;
            }
            CompleteDrawerContentLoadingIndicator();
            RestoreDrawerContentOpacity();
            ClearDrawerWidthPlaceholder();
            return;
        }

        if (_activeDrawerContentLoadRequestId != loadRequestId)
        {
            CompleteDrawerContentLoadingIndicator();
            StartDrawerContentLoadingIndicator(loadRequestId);
        }
        ApplyDrawerWidthPlaceholder(ActualDrawerWidth);
        DispatcherTimer.RunOnce(
            () => BeginDrawerContentLoad(operationId, loadRequestId),
            DrawerContentLoadDelay);
    }

    private void BeginDrawerContentLoad(int operationId, int loadRequestId)
    {
        if (!IsDrawerContentLoadCurrent(operationId, loadRequestId))
        {
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            return;
        }

        if (_activeDrawerContentLoadRequestId != loadRequestId &&
            !StartDrawerContentLoadingIndicator(loadRequestId))
        {
            Dispatcher.UIThread.Post(
                () => ContinueDrawerContentLoadAfterIndicatorReady(operationId, loadRequestId),
                DispatcherPriority.Render);
            return;
        }

        QueueDrawerContentLoad(operationId, loadRequestId);
    }

    private void ContinueDrawerContentLoadAfterIndicatorReady(int operationId, int loadRequestId)
    {
        if (!IsDrawerContentLoadCurrent(operationId, loadRequestId))
        {
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            return;
        }

        if (_activeDrawerContentLoadRequestId != loadRequestId)
        {
            StartDrawerContentLoadingIndicator(loadRequestId);
        }
        QueueDrawerContentLoad(operationId, loadRequestId);
    }

    private void QueueDrawerContentLoad(int operationId, int loadRequestId)
    {
        Dispatcher.UIThread.Post(
            () => LoadDrawerContent(operationId, loadRequestId),
            DispatcherPriority.Background);
    }

    private void LoadDrawerContent(int operationId, int loadRequestId)
    {
        if (!IsDrawerContentLoadCurrent(operationId, loadRequestId))
        {
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            return;
        }

        PrepareDrawerContentFadeInAnimation();
        try
        {
            _drawerContentPresenter!.Content = DrawerContent;
            _drawerContentPresenter.UpdateChild();
            _drawerContentPresenter.IsVisible = true;
            ClearDrawerWidthPlaceholder();
        }
        catch
        {
            _drawerContentPresenter!.IsVisible = true;
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            RestoreDrawerContentOpacity();
            throw;
        }

        if (DrawerContent == null)
        {
            CompleteDrawerContentLoadingIndicator(loadRequestId);
            RestoreDrawerContentOpacity();
            return;
        }

        Dispatcher.UIThread.Post(
            () => CompleteDrawerContentLoad(loadRequestId),
            DispatcherPriority.Background);
    }

    private bool IsDrawerContentLoadCurrent(int operationId, int loadRequestId)
    {
        return operationId == _drawerOpenOperationId &&
               loadRequestId == _drawerContentLoadRequestId &&
               IsDrawerOpen &&
               _drawerContentPresenter != null;
    }

    private void CompleteDrawerContentLoad(int loadRequestId)
    {
        if (loadRequestId == 0 ||
            loadRequestId != _drawerContentLoadRequestId ||
            !IsDrawerOpen)
        {
            if (loadRequestId != 0 &&
                loadRequestId == _activeDrawerContentLoadRequestId &&
                (loadRequestId != _drawerContentLoadRequestId || !IsDrawerOpen))
            {
                CompleteDrawerContentLoadingIndicator(loadRequestId);
                RestoreDrawerContentOpacity();
            }
            return;
        }

        CompleteDrawerContentLoadingIndicator();
        PlayDrawerContentFadeInAnimation();
    }

    private void UpdateDrawerContentPresentation()
    {
        if (_drawerContentPresenter == null)
        {
            return;
        }

        if (!IsDrawerOpen)
        {
            return;
        }

        if (_isDrawerOpeningPending || IsDrawerContentCurrent())
        {
            if (!_isDrawerOpeningPending)
            {
                _drawerContentPresenter.IsVisible = true;
            }
            return;
        }

        _drawerContentPresenter.IsVisible = false;
        ClearStaleDrawerContent();
        UpdateDeferredDrawerWidth();
        if (DrawerContent == null)
        {
            CompleteDrawerContentLoadingIndicator();
            RestoreDrawerContentOpacity();
            ClearDrawerWidthPlaceholder();
        }
        else
        {
            ScheduleDrawerContentLoad(_drawerOpenOperationId);
        }
    }

    private bool IsDrawerContentCurrent()
    {
        return _drawerContentPresenter != null &&
               Equals(_drawerContentPresenter.Content, DrawerContent);
    }

    private void ClearStaleDrawerContent()
    {
        if (_drawerContentPresenter != null && !IsDrawerContentCurrent())
        {
            _drawerContentPresenter.Content = null;
        }
    }

    private void CancelPendingDrawerOpening()
    {
        ++_drawerOpenOperationId;
        ++_drawerContentLoadRequestId;
        _isDrawerOpeningPending = false;
        if (_drawerContentPresenter != null)
        {
            _drawerContentPresenter.IsVisible = true;
        }
        CompleteDrawerContentLoadingIndicator();
        RestoreDrawerContentOpacity();
    }

    private void UpdateDeferredDrawerWidth()
    {
        var width = GetDeferredDrawerWidth();
        if (width is not > 0)
        {
            return;
        }

        ActualDrawerWidth = width.Value;
        if (_drawerContentPresenter?.Content == null)
        {
            ApplyDrawerWidthPlaceholder(width.Value);
        }
        if (_isDrawerOpeningPending && _drawerContentVisual is { } visual &&
            Math.Abs(visual.Translation.X) < 0.01)
        {
            SetDrawerOffset(GetClosedDrawerOffset(), animate: false);
        }
    }

    private double? GetDeferredDrawerWidth()
    {
        if (DrawerContent is not Control control)
        {
            return null;
        }

        if (!double.IsNaN(control.Width) && !double.IsInfinity(control.Width) && control.Width > 0)
        {
            return control.Width + control.Margin.Left + control.Margin.Right;
        }

        return control.DesiredSize.Width > 0 ? control.DesiredSize.Width : null;
    }

    private void ApplyDrawerWidthPlaceholder(double width)
    {
        if (_drawerContentBorder == null || width <= 0)
        {
            return;
        }

        if (!_isDrawerWidthPlaceholderActive)
        {
            _drawerContentBorderMinWidth = _drawerContentBorder.MinWidth;
            _isDrawerWidthPlaceholderActive = true;
        }

        _drawerContentBorder.SetCurrentValue(
            MinWidthProperty,
            Math.Max(_drawerContentBorderMinWidth, width));
    }

    private void ClearDrawerWidthPlaceholder()
    {
        if (!_isDrawerWidthPlaceholderActive || _drawerContentBorder == null)
        {
            return;
        }

        _drawerContentBorder.SetCurrentValue(MinWidthProperty, _drawerContentBorderMinWidth);
        _isDrawerWidthPlaceholderActive = false;
    }

    private bool StartDrawerContentLoadingIndicator(int loadRequestId)
    {
        if (_drawerLoadingIndicator == null)
        {
            return false;
        }

        if (_drawerLoadingIndicatorHost != null)
        {
            _drawerLoadingIndicatorHost.IsVisible = true;
        }
        _drawerLoadingIndicator.IsActive = true;
        _drawerLoadingIndicatorVisual ??= _drawerLoadingIndicatorHost == null
            ? null
            : ElementComposition.GetElementVisual(_drawerLoadingIndicatorHost);
        if (_drawerLoadingIndicatorVisual == null)
        {
            return false;
        }

        _activeDrawerContentLoadRequestId = loadRequestId;
        var visual = _drawerLoadingIndicatorVisual;
        visual.StopAnimation(nameof(visual.Opacity));
        visual.Opacity = 0;

        var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
        animation.Target = nameof(visual.Opacity);
        animation.DelayTime = DrawerLoadingIndicatorDelay;
        animation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        animation.Duration = IThemeService.AnimationLevel >= 1
            ? AnimationDuration
            : TimeSpan.FromMilliseconds(1);
        animation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        animation.InsertKeyFrame(0f, 0f);
        animation.InsertKeyFrame(1f, 1f, AnimationEasing);
        visual.StartAnimation(nameof(visual.Opacity), animation);
        return true;
    }

    private void CompleteDrawerContentLoadingIndicator(int loadRequestId = 0)
    {
        if (loadRequestId != 0 && loadRequestId != _activeDrawerContentLoadRequestId)
        {
            return;
        }

        _activeDrawerContentLoadRequestId = 0;
        if (_drawerLoadingIndicatorHost != null)
        {
            _drawerLoadingIndicatorHost.IsVisible = false;
        }
        if (_drawerLoadingIndicatorVisual is { } visual)
        {
            visual.StopAnimation(nameof(visual.Opacity));
            visual.Opacity = 0;
        }
    }

    private void PrepareDrawerContentFadeInAnimation()
    {
        if (_drawerContentPresenterVisual is not { } visual)
        {
            return;
        }

        visual.StopAnimation(nameof(visual.Opacity));
        visual.Opacity = IThemeService.AnimationLevel >= 1 ? 0 : 1;
    }

    private void RestoreDrawerContentOpacity()
    {
        if (_drawerContentPresenterVisual is not { } visual)
        {
            return;
        }

        visual.StopAnimation(nameof(visual.Opacity));
        visual.Opacity = 1;
    }

    private void PlayDrawerContentFadeInAnimation()
    {
        if (IThemeService.AnimationLevel < 1 || _drawerContentPresenterVisual == null || DrawerContent == null)
        {
            return;
        }

        var visual = _drawerContentPresenterVisual;
        visual.StopAnimation(nameof(visual.Opacity));
        visual.Opacity = 1;
        var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
        animation.Target = nameof(visual.Opacity);
        animation.Duration = AnimationDuration;
        animation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        animation.InsertKeyFrame(0f, 0f);
        animation.InsertKeyFrame(1f, 1f, AnimationEasing);
        visual.StartAnimation(nameof(visual.Opacity), animation);
    }

    public static readonly FuncValueConverter<double, double> NegativeDoubleConverter =
        new FuncValueConverter<double, double>(x => -x);

    private void SetupCompositionAnimations()
    {
        LayoutUpdated -= OnInitialLayoutUpdated;
        ClearCompositionAnimations();
        if (_contentPresenter == null || _ignoreLayer == null || _drawerContentBorder == null)
        {
            return;
        }

        _contentPresenterVisual = ElementComposition.GetElementVisual(_contentPresenter);
        _drawerContentPresenterVisual = _drawerContentPresenter == null
            ? null
            : ElementComposition.GetElementVisual(_drawerContentPresenter);
        _drawerLoadingIndicatorVisual = _drawerLoadingIndicatorHost == null
            ? null
            : ElementComposition.GetElementVisual(_drawerLoadingIndicatorHost);
        _ignoreLayerVisual = ElementComposition.GetElementVisual(_ignoreLayer);
        _drawerContentVisual = ElementComposition.GetElementVisual(_drawerContentBorder);
        if (_contentPresenterVisual == null || _ignoreLayerVisual == null || _drawerContentVisual == null)
        {
            ClearCompositionAnimations();
            return;
        }

        if (_drawerContentBorder.Bounds.Width > 0)
        {
            ActualDrawerWidth = _drawerContentBorder.Bounds.Width;
        }
        else
        {
            UpdateDeferredDrawerWidth();
        }
        UpdateDrawerOffset(animate: false);
        UpdateDrawerContentOffset();
        PseudoClasses.Set(":composition-ready", true);
        if (IThemeService.AnimationLevel < 1)
        {
            UpdateDrawerContentPresentation();
            return;
        }

        _drawerContentVisual.ImplicitAnimations = CreateImplicitAnimations(
            _drawerContentVisual,
            animateOffset: false,
            animateTranslation: true,
            animateOpacity: false);
        _ignoreLayerVisual.ImplicitAnimations = CreateImplicitAnimations(
            _ignoreLayerVisual, animateOffset: false, animateOpacity: true);
        if (IThemeService.AnimationLevel >= 2)
        {
            _contentPresenterVisual.ImplicitAnimations = CreateImplicitAnimations(
                _contentPresenterVisual,
                animateOffset: false,
                animateTranslation: true,
                animateOpacity: false);
        }

        UpdateDrawerContentPresentation();
    }

    private static ImplicitAnimationCollection CreateImplicitAnimations(
        CompositionVisual visual,
        bool animateOffset,
        bool animateOpacity,
        bool animateTranslation = false)
    {
        var animations = visual.Compositor.CreateImplicitAnimationCollection();
        if (animateOffset)
        {
            var offsetAnimation = visual.Compositor.CreateVector3DKeyFrameAnimation();
            offsetAnimation.Target = nameof(visual.Offset);
            offsetAnimation.Duration = AnimationDuration;
            offsetAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            offsetAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", AnimationEasing);
            animations[nameof(visual.Offset)] = offsetAnimation;
        }

        if (animateTranslation)
        {
            var translationAnimation = visual.Compositor.CreateVector3DKeyFrameAnimation();
            translationAnimation.Target = nameof(visual.Translation);
            translationAnimation.Duration = AnimationDuration;
            translationAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            translationAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", AnimationEasing);
            animations[nameof(visual.Translation)] = translationAnimation;
        }

        if (animateOpacity)
        {
            var opacityAnimation = visual.Compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.Target = nameof(visual.Opacity);
            opacityAnimation.Duration = AnimationDuration;
            opacityAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            opacityAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", AnimationEasing);
            animations[nameof(visual.Opacity)] = opacityAnimation;
        }

        return animations;
    }

    private void ClearCompositionAnimations()
    {
        CompleteDrawerContentLoadingIndicator();
        RestoreDrawerContentOpacity();
        PseudoClasses.Set(":composition-ready", false);
        ClearCompositionAnimations(
            _contentPresenterVisual,
            animateOffset: false,
            animateTranslation: true,
            animateOpacity: false);
        ClearCompositionAnimations(
            _drawerContentPresenterVisual,
            animateOffset: false,
            animateOpacity: true);
        ClearCompositionAnimations(
            _drawerLoadingIndicatorVisual,
            animateOffset: false,
            animateOpacity: true);
        ClearCompositionAnimations(_ignoreLayerVisual, animateOffset: false, animateOpacity: true);
        ClearCompositionAnimations(
            _drawerContentVisual,
            animateOffset: false,
            animateTranslation: true,
            animateOpacity: false);
        _contentPresenterVisual = null;
        _drawerContentPresenterVisual = null;
        _drawerLoadingIndicatorVisual = null;
        _ignoreLayerVisual = null;
        _drawerContentVisual = null;
    }

    private static void ClearCompositionAnimations(
        CompositionVisual? visual,
        bool animateOffset,
        bool animateOpacity,
        bool animateTranslation = false)
    {
        if (visual == null)
        {
            return;
        }

        if (animateOffset)
        {
            visual.StopAnimation(nameof(visual.Offset));
        }
        if (animateTranslation)
        {
            visual.StopAnimation(nameof(visual.Translation));
        }
        if (animateOpacity)
        {
            visual.StopAnimation(nameof(visual.Opacity));
        }
        visual.ImplicitAnimations?.Clear();
    }

    private void UpdateDrawerContentOffset()
    {
        if (_contentPresenterVisual == null)
        {
            return;
        }

        _contentPresenterVisual.Translation = _contentPresenterVisual.Translation with
        {
            X = GetDrawerContentOffset()
        };
    }

    private void UpdateDrawerOffset(bool animate)
    {
        if (_drawerContentVisual == null)
        {
            return;
        }

        var targetOffset = IsDrawerOpen
            ? 0
            : GetClosedDrawerOffset();
        SetDrawerOffset(targetOffset, animate);
    }

    private double GetClosedDrawerOffset()
    {
        return ActualDrawerWidth * (DrawerPlacement == DrawerPlacementEnum.Left ? -1 : 1);
    }

    private void SetDrawerOffset(double targetOffset, bool animate)
    {
        if (_drawerContentVisual == null)
        {
            return;
        }

        if (animate || IThemeService.AnimationLevel < 1)
        {
            _drawerContentVisual.Translation = _drawerContentVisual.Translation with { X = targetOffset };
            return;
        }

        var implicitAnimations = _drawerContentVisual.ImplicitAnimations;
        _drawerContentVisual.StopAnimation(nameof(_drawerContentVisual.Translation));
        _drawerContentVisual.ImplicitAnimations =
            _drawerContentVisual.Compositor.CreateImplicitAnimationCollection();
        _drawerContentVisual.Translation = _drawerContentVisual.Translation with { X = targetOffset };
        _drawerContentVisual.ImplicitAnimations = implicitAnimations;
    }

    private double GetDrawerContentOffset()
    {
        return IThemeService.AnimationLevel >= 2 && IsDrawerOpen
            ? ActualDrawerWidth * (DrawerPlacement == DrawerPlacementEnum.Left ? 0.07325 : -0.07325)
            : 0;
    }

    public enum DrawerPlacementEnum
    {
        Left,
        Right
    }
}
