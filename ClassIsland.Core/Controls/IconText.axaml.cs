using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public partial class IconText : UserControl
{
    public static readonly StyledProperty<string> GlyphProperty = AvaloniaProperty.Register<IconText, string>(
        nameof(Glyph));

    public string Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<IconText, string>(
        nameof(Text));

    public static readonly StyledProperty<Symbol> SymbolProperty = AvaloniaProperty.Register<IconText, Symbol>(
        nameof(Symbol));

    public Symbol Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<IconText, double>(
        nameof(Spacing));

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public static readonly StyledProperty<bool> UseFontIconProperty = AvaloniaProperty.Register<IconText, bool>(
        nameof(UseFontIcon), true);

    public bool UseFontIcon
    {
        get => GetValue(UseFontIconProperty);
        set => SetValue(UseFontIconProperty, value);
    }
    
    
    public IconText()
    {
        InitializeComponent();
    }
}