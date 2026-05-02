using Avalonia;
using Avalonia.Controls;

namespace ClassIsland.Core.Controls;

public class HighlightBox : ContentControl
{
    public static readonly StyledProperty<bool>? IsHighlightProperty = AvaloniaProperty.Register<HighlightBox, bool>(
        nameof(IsHighlight));

    public bool IsHighlight
    {
        get => GetValue(IsHighlightProperty);
        set => SetValue(IsHighlightProperty, value);
    }
}