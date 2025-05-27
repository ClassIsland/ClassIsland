using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Material.Icons;

namespace ClassIsland.Core.Controls;

public partial class SettingsControl : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> IconGlyphProperty = AvaloniaProperty.Register<SettingsControl, MaterialIconKind>(
        nameof(IconGlyph));

    public MaterialIconKind IconGlyph
    {
        get => GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<SettingsControl, string>(
        nameof(Header));

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<SettingsControl, string>(
        nameof(Description));

    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly StyledProperty<object> SwitcherProperty = AvaloniaProperty.Register<SettingsControl, object>(
        nameof(Switcher));

    public object Switcher
    {
        get => GetValue(SwitcherProperty);
        set => SetValue(SwitcherProperty, value);
    }

    public static readonly StyledProperty<bool> HasSwitcherProperty = AvaloniaProperty.Register<SettingsControl, bool>(
        nameof(HasSwitcher), true);

    public bool HasSwitcher
    {
        get => GetValue(HasSwitcherProperty);
        set => SetValue(HasSwitcherProperty, value);
    }

    public static readonly StyledProperty<bool> IsOnProperty = AvaloniaProperty.Register<SettingsControl, bool>(
        nameof(IsOn));

    public bool IsOn
    {
        get => GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<SettingsControl, bool>(
        nameof(IsCompact));

    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }
    
    public SettingsControl()
    {
        InitializeComponent();
    }
}