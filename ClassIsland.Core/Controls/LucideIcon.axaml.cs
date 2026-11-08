using Avalonia;
using ClassIsland.Core.Icons;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class LucideIcon : FAFontIcon
{
    public static readonly StyledProperty<LucideIconKind?> IconProperty =
        AvaloniaProperty.Register<LucideIcon, LucideIconKind?>(nameof(Icon));

    public LucideIconKind? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public LucideIcon()
    {
        FontFamily = AppBase.LucideIconsFontFamily;
    }
    
    public LucideIcon(string glyph) : this()
    {
        Glyph = glyph;
    }
    
    public LucideIcon(string glyph, double size) : this(glyph)
    {
        Width = Height = size;
    }
    
    public object ProvideValue() => this;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
        {
            var kind = change.GetNewValue<LucideIconKind?>();
            if (kind.HasValue)
                Glyph = char.ConvertFromUtf32((int)kind.Value);
        }
    }
}