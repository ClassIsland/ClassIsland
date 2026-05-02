using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class LucideIcon : FontIcon
{
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
}