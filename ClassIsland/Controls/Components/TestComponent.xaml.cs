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
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Controls.Components;

/// <summary>
/// TestComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("EE8F66BD-C423-4E7C-AB46-AA9976B00E08", "测试组件")]
public partial class TestComponent : ComponentBase
{
    public TestComponent()
    {
        InitializeComponent();
    }
}