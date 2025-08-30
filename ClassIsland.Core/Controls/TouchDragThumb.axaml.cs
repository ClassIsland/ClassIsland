using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace ClassIsland.Core.Controls;

public class TouchDragThumb : Thumb
{
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<TouchDragThumb, Orientation>(
        nameof(Orientation), Orientation.Vertical);

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<TouchDragThumb, bool>(
        nameof(IsCompact));

    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }
}