using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// SF Symbols 图标源
/// </summary>
public class SFSymbolsIconSource : FontIconSource
{
    public SFSymbolsIconSource()
    {
        FontFamily = AppBase.SFSymbolsFontFamily;
    }

    public SFSymbolsIconSource(string glyph) : this()
    {
        Glyph = glyph;
    }

    public SFSymbolsIconSource ProvideValue() => this;
}
