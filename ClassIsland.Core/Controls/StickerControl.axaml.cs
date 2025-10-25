using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class StickerControl : TemplatedControl
{
    public static readonly StyledProperty<IImage?> StickerSourceProperty = AvaloniaProperty.Register<StickerControl, IImage?>(
        nameof(StickerSource));

    public IImage? StickerSource
    {
        get => GetValue(StickerSourceProperty);
        set => SetValue(StickerSourceProperty, value);
    }

    public static readonly StyledProperty<string?> StickerToolTipProperty = AvaloniaProperty.Register<StickerControl, string?>(
        nameof(StickerToolTip));

    public string? StickerToolTip
    {
        get => GetValue(StickerToolTipProperty);
        set => SetValue(StickerToolTipProperty, value);
    }

    public static readonly StyledProperty<IconSource?> FallbackIconProperty = AvaloniaProperty.Register<StickerControl, IconSource?>(
        nameof(FallbackIcon));

    public IconSource? FallbackIcon
    {
        get => GetValue(FallbackIconProperty);
        set => SetValue(FallbackIconProperty, value);
    }
}