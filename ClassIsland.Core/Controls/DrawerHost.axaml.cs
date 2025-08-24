using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Controls;

[PseudoClasses(":drawer-left", ":drawer-right", ":open", ":animated")]
[TemplatePart("PART_IgnoreLayer", typeof(Border))]
[TemplatePart("PART_DrawerContentBorder", typeof(Border))]
public class DrawerHost : ContentControl
{

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

    private Border? _ignoreLayer;
    private Border? _drawerContentBorder;

    public DrawerHost()
    {
        if (IThemeService.AnimationLevel >= 1)
        {
            PseudoClasses.Set(":animated", true);
        }
        this.GetObservable(DrawerPlacementProperty).Subscribe(_ => UpdateDrawerPlacement());
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
        if (_drawerContentBorder != null) 
            _drawerContentBorder.SizeChanged -= DrawerContentBorderOnSizeChanged;
        if (_ignoreLayer != null) 
            _ignoreLayer.PointerPressed -= IgnoreLayerOnPointerPressed;

        _ignoreLayer = this.GetTemplateChildren().OfType<Border>().FirstOrDefault(x => x.Name == "PART_IgnoreLayer");
        _drawerContentBorder = this.GetTemplateChildren().OfType<Border>().FirstOrDefault(x => x.Name == "PART_DrawerContentBorder");

        if (_drawerContentBorder != null) 
            _drawerContentBorder.SizeChanged += DrawerContentBorderOnSizeChanged;
        if (_ignoreLayer != null) 
            _ignoreLayer.PointerPressed += IgnoreLayerOnPointerPressed;

        base.OnApplyTemplate(e);
    }

    private void IgnoreLayerOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        IsDrawerOpen = false;
    }

    private void DrawerContentBorderOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ActualDrawerWidth = e.NewSize.Width;
    }

    public static readonly FuncValueConverter<double, double> NegativeDoubleConverter =
        new FuncValueConverter<double, double>(x => -x);

    public enum DrawerPlacementEnum
    {
        Left,
        Right
    }
}