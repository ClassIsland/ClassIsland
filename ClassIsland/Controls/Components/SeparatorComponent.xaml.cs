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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// SeparatorComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("AB0F26D5-9DF6-4575-B844-73B04D0907C1", "分割线", PackIconKind.ArrowSplitVertical, "显示一个分割线，视觉上对组件进行分组。")]
public partial class SeparatorComponent
{
    public SeparatorComponent()
    {
        InitializeComponent();
    }
}