using Avalonia;
using ClassIsland.Core.Icons;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// Lucide Icon 图标源
/// </summary>
public class LucideIconSource : FAFontIconSource
{
    public static readonly StyledProperty<LucideIconKind?> IconProperty =
        AvaloniaProperty.Register<LucideIconSource, LucideIconKind?>(nameof(Icon));

    public LucideIconKind? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
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