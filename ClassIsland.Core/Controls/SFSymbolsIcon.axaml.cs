using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class SFSymbolsIcon : FontIcon
{
    public SFSymbolsIcon()
    {
        FontFamily = AppBase.SFSymbolsFontFamily;
    }

    public SFSymbolsIcon(string glyph) : this()
    {
        Glyph = glyph;
    }

    public SFSymbolsIcon(string glyph, double size) : this(glyph)
    {
        Width = Height = size;
    }

    public object ProvideValue() => this;
}
