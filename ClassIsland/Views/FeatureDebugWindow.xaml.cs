using System.Windows;

using ClassIsland.Controls;
using ClassIsland.Core.Controls;

namespace ClassIsland.Views;

/// <summary>
/// FeatureDebugWindow.xaml 的交互逻辑
/// </summary>
public partial class FeatureDebugWindow : MyWindow
{
    public FeatureDebugWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void ButtonPlayEffect_OnClick(object sender, RoutedEventArgs e)
    {
        RippleEffect.Play();
    }
}