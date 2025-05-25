using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Controls;
/// <summary>
/// SettingsCard.xaml 的交互逻辑
/// </summary>
public partial class SettingsCard : UserControl
{
    public static readonly DependencyProperty IconGlyphProperty = DependencyProperty.Register(nameof(IconGlyph), typeof(PackIconKind), typeof(SettingsCard), new PropertyMetadata(PackIconKind.SimpleIcons));
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsCard), new PropertyMetadata(""));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata(""));
    public static readonly DependencyProperty SwitcherProperty = DependencyProperty.Register(nameof(Switcher), typeof(object), typeof(SettingsCard), new PropertyMetadata(null));
    public static readonly DependencyProperty HasSwitcherProperty = DependencyProperty.Register(nameof(HasSwitcher), typeof(bool), typeof(SettingsCard), new PropertyMetadata(true));
    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(SettingsCard), new PropertyMetadata(false));

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

    public bool HasCustomSwitcher => Switcher != null;

    public SettingsCard()
    {
        InitializeComponent();
    }
}
