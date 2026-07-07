using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class FluentIcon : FontIcon
{
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
}