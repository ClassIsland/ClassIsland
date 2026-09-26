using Avalonia;
using Avalonia.Media;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Enums.UI;
using CompositionMaterial.Avalonia;
using CompositionMaterial.Avalonia.Materials;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 为主界面背景提供可回退的系统合成材质。
/// </summary>
public class MainWindowBackgroundMaterialControl : CompositionMaterialControl
{
    public static readonly DirectProperty<MainWindowBackgroundMaterialControl, double> EffectiveBackgroundOpacityProperty =
        AvaloniaProperty.RegisterDirect<MainWindowBackgroundMaterialControl, double>(
            nameof(EffectiveBackgroundOpacity), control => control._effectiveBackgroundOpacity);

    private readonly AcrylicMaterial _acrylicMaterial = new();
    private readonly LiquidGlassMaterial _liquidGlassMaterial = new();
    private readonly MicaMaterial _micaMaterial = new();
    private double _effectiveBackgroundOpacity = 0.5;

    public double EffectiveBackgroundOpacity => _effectiveBackgroundOpacity;

    public MainWindowBackgroundMaterialControl()
    {
        UpdateMaterial();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FallbackBrushProperty ||
            change.Property == MainWindowStylesAssist.IsBackgroundMaterialEnabledProperty ||
            change.Property == MainWindowStylesAssist.BackgroundMaterialTypeProperty ||
            change.Property == MainWindowStylesAssist.BackgroundOpacityProperty)
        {
            UpdateMaterial();
        }
        else if (change.Property == IsNativeMaterialActiveProperty)
        {
            UpdateEffectiveBackgroundOpacity();
        }
    }

    private void UpdateMaterial()
    {
        var backgroundOpacity = Math.Clamp(MainWindowStylesAssist.GetBackgroundOpacity(this), 0, 1);
        _acrylicMaterial.TintOpacity = backgroundOpacity;
        _liquidGlassMaterial.SurfaceOpacity = backgroundOpacity;
        _micaMaterial.TintOpacity = backgroundOpacity;

        if (FallbackBrush is ISolidColorBrush solidColorBrush)
        {
            _acrylicMaterial.TintColor = solidColorBrush.Color;
            _liquidGlassMaterial.TintColor = solidColorBrush.Color;
            _micaMaterial.TintColor = solidColorBrush.Color;
        }

        if (!MainWindowStylesAssist.GetIsBackgroundMaterialEnabled(this))
        {
            Material = null;
            UpdateEffectiveBackgroundOpacity();
            return;
        }

        Material = MainWindowStylesAssist.GetBackgroundMaterialType(this) switch
        {
            MainWindowBackgroundMaterialType.LiquidGlass => _liquidGlassMaterial,
            MainWindowBackgroundMaterialType.Mica => _micaMaterial,
            _ => _acrylicMaterial
        };
        UpdateEffectiveBackgroundOpacity();
    }

    private void UpdateEffectiveBackgroundOpacity()
    {
        var backgroundOpacity = Math.Clamp(MainWindowStylesAssist.GetBackgroundOpacity(this), 0, 1);
        var opacity = MainWindowStylesAssist.GetIsBackgroundMaterialEnabled(this) && IsNativeMaterialActive
            ? 1
            : backgroundOpacity;
        SetAndRaise(EffectiveBackgroundOpacityProperty, ref _effectiveBackgroundOpacity, opacity);
    }
}
