using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Icons;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public partial class IconText : UserControl
{
    public static readonly StyledProperty<FAIconSource?> IconSourceProperty =
        AvaloniaProperty.Register<IconText, FAIconSource?>(nameof(IconSource));

    public FAIconSource? IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public static readonly StyledProperty<string?> GlyphProperty = AvaloniaProperty.Register<IconText, string?>(
        nameof(Glyph));

    public string? Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
    
    public static readonly StyledProperty<FluentIconKind?> IconProperty =
        AvaloniaProperty.Register<IconText, FluentIconKind?>(nameof(Icon));

    public FluentIconKind? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<IconText, string>(
        nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<IconText, double>(
        nameof(Spacing), 4);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }
    
    
    public IconText()
    {
        InitializeComponent();
    }

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
