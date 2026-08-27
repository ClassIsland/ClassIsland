using Avalonia;
using Avalonia.Media;
using CompositionMaterial.Avalonia;
using CompositionMaterial.Avalonia.Materials;
using FluentAvalonia.Styling;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 为弹出层背景提供可回退的 WinUI 亚克力材质。
/// </summary>
public class PopupAcrylicMaterialControl : CompositionMaterialControl
{
    private readonly AcrylicMaterial _acrylicMaterial = new();

    public PopupAcrylicMaterialControl()
    {
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
        UpdateMaterial();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FallbackBrushProperty)
        {
            UpdateMaterial();
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateMaterial();
    }

    private void UpdateMaterial()
    {
        if (FallbackBrush is null)
        {
            Material = null;
            return;
        }

        if (FallbackBrush is ISolidColorBrush solidColorBrush)
        {
            _acrylicMaterial.TintColor = solidColorBrush.Color;
        }

        Material = ActualThemeVariant == FluentAvaloniaTheme.HighContrastTheme
            ? null
            : _acrylicMaterial;
    }
}
