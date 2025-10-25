using System.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;


namespace ClassIsland.Core.Controls;
/// <summary>
/// StarRailLoadingControl.xaml 的交互逻辑
/// </summary>
public partial class StarRailLoadingControl : UserControl
{
    private bool _isPlayed = false;
    

    public StarRailLoadingControl()
    {
        InitializeComponent();
    }

    private void PART_ControlRoot_Loaded(object sender, RoutedEventArgs e)
    {
    }
}


