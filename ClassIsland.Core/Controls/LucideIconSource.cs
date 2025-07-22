using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// Lucide Icon 图标源
/// </summary>
public class LucideIconSource : FontIconSource
{
    /// <inheritdoc />
    public LucideIconSource()
    {
        FontFamily = AppBase.LucideIconsFontFamily;
    }
    /// <inheritdoc />
    public LucideIconSource(string glyph) : this()
    {
        Glyph = glyph;
    }

    public LucideIconSource ProvideValue() => this;
}