using System.Windows;
using System.Windows.Controls;

using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls;

/// <summary>
/// SettingsControl.xaml 的交互逻辑
/// </summary>
public partial class SettingsControl : UserControl
{
    public static readonly DependencyProperty IconGlyphProperty = DependencyProperty.Register(nameof(IconGlyph), typeof(PackIconKind), typeof(SettingsControl), new PropertyMetadata(PackIconKind.SimpleIcons));
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsControl), new PropertyMetadata(""));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsControl), new PropertyMetadata(""));
    public static readonly DependencyProperty SwitcherProperty = DependencyProperty.Register(nameof(Switcher), typeof(object), typeof(SettingsControl), new PropertyMetadata(null));
    public static readonly DependencyProperty HasSwitcherProperty = DependencyProperty.Register(nameof(HasSwitcher), typeof(bool), typeof(SettingsControl), new PropertyMetadata(true));
    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(SettingsControl), new PropertyMetadata(false));
    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(SettingsControl), new PropertyMetadata(false));

    public PackIconKind IconGlyph
    {
        get => (PackIconKind)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool HasSwitcher
    {
        get => (bool)GetValue(HasSwitcherProperty);
        set => SetValue(HasSwitcherProperty, value);
    }

    public object? Switcher
    {
        get => GetValue(SwitcherProperty);
        set => SetValue(SwitcherProperty, value);
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public bool HasCustomSwitcher => Switcher != null;

    public SettingsControl()
    {
        InitializeComponent();
    }
}