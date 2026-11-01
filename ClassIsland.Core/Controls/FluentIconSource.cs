using Avalonia;
using ClassIsland.Core.Icons;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// Fluent Icon 图标源
/// </summary>
public class FluentIconSource : FAFontIconSource
{
    public static readonly StyledProperty<FluentIconKind?> IconProperty =
        AvaloniaProperty.Register<FluentIconSource, FluentIconKind?>(nameof(Icon));

    public FluentIconKind? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
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