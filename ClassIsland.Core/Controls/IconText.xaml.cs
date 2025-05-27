using System.Windows;
using System.Windows.Controls;

namespace ClassIsland.Core.Controls;

/// <summary>
/// IconText.xaml 的交互逻辑
/// </summary>
public partial class IconText : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(IconText), new PropertyMetadata(default(string)));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(MaterialIconKind), typeof(IconText), new PropertyMetadata(default(MaterialIconKind)));

    public MaterialIconKind Kind
    {
        get => (MaterialIconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register(
        nameof(IconMargin), typeof(Thickness), typeof(IconText), new PropertyMetadata(new Thickness(6, 0, 0, 0)));

    public Thickness IconMargin
    {
        get => (Thickness)GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public IconText()
    {
        InitializeComponent();
    }
}