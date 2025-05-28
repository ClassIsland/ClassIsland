using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Material.Icons;

namespace ClassIsland.Core.Controls.IconControl;

/// <summary>
/// 图标控件。用于显示位图图标或MD图标包图标，并支持在图标不可用时显示指定的默认图标。
/// </summary>
[TemplatePart(Name = "PART_ImageIcon", Type = typeof(Image))]
[PseudoClasses(":material-icon", ":image-icon")]
public class IconControl : TemplatedControl
{
    #region Props

    public static readonly StyledProperty<MaterialIconKind> MaterialIconKindProperty = AvaloniaProperty.Register<IconControl, MaterialIconKind>(
        nameof(MaterialIconKind));

    public MaterialIconKind MaterialIconKind
    {
        get => GetValue(MaterialIconKindProperty);
        set => SetValue(MaterialIconKindProperty, value);
    }

    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
        nameof(ImageSource), typeof(ImageSource), typeof(IconControl), new PropertyMetadata(default(ImageSource),
            (o, args) =>
            {
                if (o is IconControl control)
                {
                    control.OnImageSourceChanged(o, args);
                }
            }));

    public ImageSource ImageSource
    {
        get { return (ImageSource)GetValue(ImageSourceProperty); }
        set { SetValue(ImageSourceProperty, value); }
    }

    public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
        nameof(IconKind), typeof(IconControlIconKind), typeof(IconControl), new PropertyMetadata(default(IconControlIconKind), (o, args) =>
        {
            if (o is IconControl control)
            {
                control.OnIconKindChanged(o, args);
            }
        }));

    public IconControlIconKind IconKind
    {
        get { return (IconControlIconKind)GetValue(IconKindProperty); }
        set { SetValue(IconKindProperty, value); }
    }

    public static readonly StyledProperty<MaterialIconKind> FallbackMaterialIconKindProperty = AvaloniaProperty.Register<IconControl, MaterialIconKind>(
        nameof(FallbackMaterialIconKind));
    public MaterialIconKind FallbackMaterialIconKind
    {
        get => GetValue(FallbackMaterialIconKindProperty);
        set => SetValue(FallbackMaterialIconKindProperty, value);
    
    }

    public static readonly StyledProperty<IconControlIconKind> RealIconKindProperty = AvaloniaProperty.Register<IconControl, IconControlIconKind>(
        nameof(RealIconKind));
    public IconControlIconKind RealIconKind
    {
        get => GetValue(RealIconKindProperty);
        set => SetValue(RealIconKindProperty, value);
    
    }

    public static readonly StyledProperty<bool> IsFallbackModeProperty = AvaloniaProperty.Register<IconControl, bool>(
        nameof(IsFallbackMode));
    public bool IsFallbackMode
    {
        get => GetValue(IsFallbackModeProperty);
        set => SetValue(IsFallbackModeProperty, value);
    
    }

    #endregion

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (GetTemplateChild("PART_ImageIcon") is Image image)
        {
            image.ImageFailed += ImageOnImageFailed;
        }
        base.OnApplyTemplate();
    }

    private void ImageOnImageFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        RealIconKind = IconControlIconKind.PackIcon;
        IsFallbackMode = true;
    }

    private void OnIconKindChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
    {
        UpdateIconSource();
    }

    private void UpdateIconSource()
    {
        Console.WriteLine(ImageSource);
        if (IconKind == IconControlIconKind.Image && ImageSource == null)
        {
            RealIconKind = IconControlIconKind.PackIcon;
            IsFallbackMode = true;
            return;
        }
        RealIconKind = IconKind;
        IsFallbackMode = false;
    }

    private void OnImageSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
    {
        UpdateIconSource();
    }
}