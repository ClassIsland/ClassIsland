using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Controls;

/// <summary>
/// InfoCard.xaml 的交互逻辑
/// </summary>
[ContentProperty("Content")]
public partial class InfoCard : UserControl
{
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(string), typeof(InfoCard), new PropertyMetadata(default(string)));

    public string Content
    {
        get { return (string)GetValue(ContentProperty); }
        set { SetValue(ContentProperty, value); }
    }

    public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
        nameof(IconKind), typeof(PackIconKind), typeof(InfoCard), new PropertyMetadata(default(PackIconKind)));

    public PackIconKind IconKind
    {
        get { return (PackIconKind)GetValue(IconKindProperty); }
        set { SetValue(IconKindProperty, value); }
    }

    public static readonly DependencyProperty HasCloseButtonProperty = DependencyProperty.Register(
        nameof(HasCloseButton), typeof(bool), typeof(InfoCard), new PropertyMetadata(default(bool)));

    public bool HasCloseButton
    {
        get { return (bool)GetValue(HasCloseButtonProperty); }
        set { SetValue(HasCloseButtonProperty, value); }
    }

    public static readonly DependencyProperty ActionButtonProperty = DependencyProperty.Register(
        nameof(ActionButton), typeof(Visual), typeof(InfoCard), new PropertyMetadata(default(Visual)));

    public Visual ActionButton
    {
        get { return (Visual)GetValue(ActionButtonProperty); }
        set { SetValue(ActionButtonProperty, value); }
    }

    public InfoCard()
    {
        InitializeComponent();
    }
}