using System.Windows;
using System.Windows.Controls;

namespace ClassIsland.Controls;

/// <summary>
/// ListDetailView.xaml 的交互逻辑
/// </summary>
public partial class ListDetailView : UserControl
{
    public static readonly DependencyProperty LeftContentProperty = DependencyProperty.Register(
        nameof(LeftContent), typeof(object), typeof(ListDetailView), new PropertyMetadata(default(object)));

    public object LeftContent
    {
        get => (object)GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public static readonly DependencyProperty RightContentProperty = DependencyProperty.Register(
        nameof(RightContent), typeof(object), typeof(ListDetailView), new PropertyMetadata(default(object)));

    public object RightContent
    {
        get => (object)GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public static readonly DependencyProperty TitleElementProperty = DependencyProperty.Register(
        nameof(TitleElement), typeof(object), typeof(ListDetailView), new PropertyMetadata(default(object)));

    public object TitleElement
    {
        get => (object)GetValue(TitleElementProperty);
        set => SetValue(TitleElementProperty, value);
    }

    public static readonly DependencyProperty IsPanelOpenedProperty = DependencyProperty.Register(
        nameof(IsPanelOpened), typeof(bool), typeof(ListDetailView), new PropertyMetadata(false));

    public bool IsPanelOpened
    {
        get => (bool)GetValue(IsPanelOpenedProperty);
        set => SetValue(IsPanelOpenedProperty, value);
    }

    public static readonly DependencyProperty MinCompressWidthProperty = DependencyProperty.Register(
        nameof(MinCompressWidth), typeof(double), typeof(ListDetailView), new PropertyMetadata(800.0));
    public double MinCompressWidth
    {
        get => (double)GetValue(MinCompressWidthProperty);
        set => SetValue(MinCompressWidthProperty, value);
    }

    public static readonly DependencyProperty IsCompressedModeProperty = DependencyProperty.Register(
        nameof(IsCompressedMode), typeof(bool), typeof(ListDetailView), new PropertyMetadata(default(bool)));

    public bool IsCompressedMode
    {
        get => (bool)GetValue(IsCompressedModeProperty);
        set => SetValue(IsCompressedModeProperty, value);
    }

    public static readonly DependencyProperty ShowTitleWhenNotCompressedProperty = DependencyProperty.Register(
        nameof(ShowTitleWhenNotCompressed), typeof(bool), typeof(ListDetailView), new PropertyMetadata(false));

    public bool ShowTitleWhenNotCompressed
    {
        get => (bool)GetValue(ShowTitleWhenNotCompressedProperty);
        set => SetValue(ShowTitleWhenNotCompressedProperty, value);
    }

    public ListDetailView()
    {
        InitializeComponent();
    }

    private void GridRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        IsCompressedMode = e.NewSize.Width <= MinCompressWidth;
    }

    private void ButtonBack_OnClick(object sender, RoutedEventArgs e)
    {
        IsPanelOpened = false;
    }
}