using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// Fluent Icon 图标源
/// </summary>
public class FluentIconSource : FontIconSource
{
    /// <inheritdoc />
    public FluentIconSource()
    {
        FontFamily = AppBase.FluentIconsFontFamily;
    }
    /// <inheritdoc />
    public FluentIconSource(string glyph) : this()
    {
        Glyph = glyph;
    }

    public FluentIconSource ProvideValue() => this;
}