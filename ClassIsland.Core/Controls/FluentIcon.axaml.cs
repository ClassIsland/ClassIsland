using Avalonia;
using ClassIsland.Core.Icons;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class FluentIcon : FAFontIcon
{
    public static readonly StyledProperty<FluentIconKind?> IconProperty =
        AvaloniaProperty.Register<FluentIcon, FluentIconKind?>(nameof(Icon));

    public FluentIconKind? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public FluentIcon()
    {
        FontFamily = AppBase.FluentIconsFontFamily;
    }
    
    public FluentIcon(string glyph) : this()
    {
        Glyph = glyph;
    }
    
    public FluentIcon(string glyph, double size) : this(glyph)
    {
        Width = Height = size;
    }
    
    public object ProvideValue() => this;
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
        {
            var kind = change.GetNewValue<FluentIconKind?>();
            if (kind.HasValue)
                Glyph = char.ConvertFromUtf32((int)kind.Value);
        }
    }
}