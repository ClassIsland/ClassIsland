using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClassIsland.Controls;

/// <summary>
/// ListDetailView.xaml 的交互逻辑
/// </summary>
public partial class ListDetailView : UserControl
{
    public static readonly StyledProperty<object> LeftContentProperty = AvaloniaProperty.Register<ListDetailView, object>(
        nameof(LeftContent));

    public object LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public static readonly StyledProperty<object> RightContentProperty = AvaloniaProperty.Register<ListDetailView, object>(
        nameof(RightContent));

    public object RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public static readonly StyledProperty<object> TitleElementProperty = AvaloniaProperty.Register<ListDetailView, object>(
        nameof(TitleElement));

    public object TitleElement
    {
        get => GetValue(TitleElementProperty);
        set => SetValue(TitleElementProperty, value);
    }

    public static readonly StyledProperty<bool> IsPanelOpenedProperty = AvaloniaProperty.Register<ListDetailView, bool>(
        nameof(IsPanelOpened), false);

    public bool IsPanelOpened
    {
        get => GetValue(IsPanelOpenedProperty);
        set => SetValue(IsPanelOpenedProperty, value);
    }

    public static readonly StyledProperty<double> MinCompressWidthProperty = AvaloniaProperty.Register<ListDetailView, double>(
        nameof(MinCompressWidth), 800.0);

    public double MinCompressWidth
    {
        get => GetValue(MinCompressWidthProperty);
        set => SetValue(MinCompressWidthProperty, value);
    }

    public static readonly StyledProperty<bool> IsCompressedModeProperty = AvaloniaProperty.Register<ListDetailView, bool>(
        nameof(IsCompressedMode), default(bool));

    public bool IsCompressedMode
    {
        get => GetValue(IsCompressedModeProperty);
        set => SetValue(IsCompressedModeProperty, value);
    }

    public static readonly StyledProperty<bool> ShowTitleWhenNotCompressedProperty = AvaloniaProperty.Register<ListDetailView, bool>(
        nameof(ShowTitleWhenNotCompressed), false);

    public bool ShowTitleWhenNotCompressed
    {
        get => GetValue(ShowTitleWhenNotCompressedProperty);
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

