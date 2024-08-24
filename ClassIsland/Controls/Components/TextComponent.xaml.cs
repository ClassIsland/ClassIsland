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
using ClassIsland;
using ClassIsland.Models.ComponentSettings;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// TestComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("EE8F66BD-C423-4E7C-AB46-AA9976B00E08", "文本", PackIconKind.TextBoxOutline, "显示自定义文本。")]
public partial class TextComponent : ComponentBase<TextComponentSettings>
{
    public TextComponent()
    {
        InitializeComponent();
    }
}