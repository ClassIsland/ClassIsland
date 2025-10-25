using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls;

public class Thing : TemplatedControl
{
    public static readonly StyledProperty<IconSource> IconSourceProperty = AvaloniaProperty.Register<Thing, IconSource>(
        nameof(IconSource));

    public IconSource IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<Thing, string>(
        nameof(Header));

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<Thing, string>(
        nameof(Description));

    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    
    
}