using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class FluentIcon : FontIcon
{
    public FluentIcon()
    {
        
    }
    
    public FluentIcon(string glyph) : base()
    {
        Glyph = glyph;
    }
    
    public object ProvideValue() => this;
}