using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Controls;

[TemplatePart("PART_Canvas", typeof(Canvas))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_TitleContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_DismissLayer", typeof(Border))]
[TemplatePart("PART_BackgroundElement", typeof(Border))]
[PseudoClasses(":opened", ":closed", ":collapsed", ":can-collapse", ":animated")]
public class VerticalDrawer : ContentControl
{
    public static FuncValueConverter<double, double> ContainerHeightToClosedCanvasBottomPosConverter { get; } =
        new(x => -x - 2);
    
    public static FuncValueConverter<double, double> ContainerHeightToCollapsedCanvasBottomPosConverter { get; } =
        new(x => -x + 40);
    
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<VerticalDrawer, object?>(
        nameof(Title));

    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty = AvaloniaProperty.Register<VerticalDrawer, IDataTemplate?>(
        nameof(TitleTemplate));

    public IDataTemplate? TitleTemplate
    {
        get => GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    public static readonly StyledProperty<double> ContentHeightRatioProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(ContentHeightRatio), 0.5);

    public double ContentHeightRatio
    {
        get => GetValue(ContentHeightRatioProperty);
        set => SetValue(ContentHeightRatioProperty, value);
    }

    public static readonly StyledProperty<bool> SmokeBackgroundProperty = AvaloniaProperty.Register<VerticalDrawer, bool>(
        nameof(SmokeBackground), true);

    public bool SmokeBackground
    {
        get => GetValue(SmokeBackgroundProperty);
        set => SetValue(SmokeBackgroundProperty, value);
    }

    public static readonly StyledProperty<VerticalDrawerOpenState> StateProperty = AvaloniaProperty.Register<VerticalDrawer, VerticalDrawerOpenState>(
        nameof(State));

    public VerticalDrawerOpenState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public static readonly StyledProperty<bool> AllowCollapseProperty = AvaloniaProperty.Register<VerticalDrawer, bool>(
        nameof(AllowCollapse));

    public bool AllowCollapse
    {
        get => GetValue(AllowCollapseProperty);
        set => SetValue(AllowCollapseProperty, value);
    }

    public static readonly StyledProperty<double> ContainerLeftPosProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(ContainerLeftPos));

    public double ContainerLeftPos
    {
        get => GetValue(ContainerLeftPosProperty);
        set => SetValue(ContainerLeftPosProperty, value);
    }

    public static readonly StyledProperty<double> ContainerHeightProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(ContainerHeight));

    public double ContainerHeight
    {
        get => GetValue(ContainerHeightProperty);
        set => SetValue(ContainerHeightProperty, value);
    }

    public static readonly StyledProperty<double> RequestedWidthProperty = AvaloniaProperty.Register<VerticalDrawer, double>(
        nameof(RequestedWidth), double.NaN);

    public double RequestedWidth
    {
        get => GetValue(RequestedWidthProperty);
        set => SetValue(RequestedWidthProperty, value);
    }

    private Border? _dismissLayer;
    private Border? _backgroundElement;
    private Canvas? _canvas;
    private ContentPresenter? _contentPresenter;
    private ContentPresenter? _titleContentPresenter;
    private IDisposable? _stateObserver;
    private IDisposable? _canCollapseObserver;

    public VerticalDrawer()
    {
        if (IThemeService.AnimationLevel >= 1)
        {
            PseudoClasses.Set(":animated", true);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _stateObserver?.Dispose();
        _canCollapseObserver?.Dispose();
        _stateObserver = this.GetObservable(StateProperty).Subscribe(_ => UpdateContent());
        _canCollapseObserver = this.GetObservable(AllowCollapseProperty).Subscribe(_ => UpdateContent());
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
            Dismiss();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _stateObserver?.Dispose();
        _canCollapseObserver?.Dispose();
        KeyDown -= OnKeyDown;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        if (_dismissLayer != null)
        {
            _dismissLayer.PointerPressed -= DismissLayerOnPointerPressed;
        }
        if (_backgroundElement != null)
        {
            _backgroundElement.SizeChanged -= BackgroundElementOnSizeChanged;
            _backgroundElement.PointerPressed -= BackgroundElementOnPointerPressed;
        }
        if (_canvas != null)
        {
            _canvas.SizeChanged -= CanvasOnSizeChanged;
        }
        
        _dismissLayer = this.GetTemplateChildren().OfType<Border>().FirstOrDefault(x => x.Name == "PART_DismissLayer");
        _backgroundElement = this.GetTemplateChildren().OfType<Border>().FirstOrDefault(x => x.Name == "PART_BackgroundElement");
        _canvas = this.GetTemplateChildren().OfType<Canvas>().FirstOrDefault(x => x.Name == "PART_Canvas");
        _contentPresenter = this.GetTemplateChildren().OfType<ContentPresenter>().FirstOrDefault(x => x.Name == "PART_ContentPresenter");
        _titleContentPresenter = this.GetTemplateChildren().OfType<ContentPresenter>().FirstOrDefault(x => x.Name == "PART_TitleContentPresenter");

        if (_dismissLayer != null)
        {
            _dismissLayer.PointerPressed += DismissLayerOnPointerPressed;
        }
        if (_backgroundElement != null)
        {
            _backgroundElement.SizeChanged += BackgroundElementOnSizeChanged;
            _backgroundElement.PointerPressed += BackgroundElementOnPointerPressed;
        }
        if (_canvas != null)
        {
            _canvas.SizeChanged += CanvasOnSizeChanged;
        }
    }

    private void BackgroundElementOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Open();
    }

    private void CanvasOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdatePos();
    }

    private void BackgroundElementOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdatePos();
    }

    private void UpdatePos()
    {
        if (_canvas == null)
        {
            return;
        }
        
        ContainerHeight = _canvas.Bounds.Height * ContentHeightRatio;
    }

    private void UpdateContent()
    {
        PseudoClasses.Set(":opened", State == VerticalDrawerOpenState.Opened);
        PseudoClasses.Set(":collapsed", State == VerticalDrawerOpenState.Collapsed);
        PseudoClasses.Set(":closed", State == VerticalDrawerOpenState.Closed);
        PseudoClasses.Set(":can-collapse", AllowCollapse);
    }

    private void DismissLayerOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Dismiss();
    }

    public void Close()
    {
        State = VerticalDrawerOpenState.Closed;
    }

    public void Collapse()
    {
        if (!AllowCollapse)
        {
            return;
        }

        State = VerticalDrawerOpenState.Collapsed;
    }

    public void Open()
    {
        State = VerticalDrawerOpenState.Opened;
    }

    public void Dismiss()
    {
        if (AllowCollapse)
        {
            Collapse();
        }
        else
        {
            Close();
        }
    }
}

public enum VerticalDrawerOpenState
{
    Closed,
    Collapsed,
    Opened
}