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
/// SlideComponent.xaml 的交互逻辑
/// </summary>
[ContainerComponent]
[ComponentInfo("7E19A113-D281-4F33-970A-834A0B78B5AD", "轮播组件", PackIconKind.Slideshow, "轮播多个组件。")]
public partial class SlideComponent
{
    public SlideComponent()
    {
        InitializeComponent();
    }
}