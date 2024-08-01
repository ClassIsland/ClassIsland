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

namespace ClassIsland.Controls;

/// <summary>
/// PluginSourceListBoxItem.xaml 的交互逻辑
/// </summary>
public partial class PluginSourceListBoxItem : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(KeyValuePair<string, string>), typeof(PluginSourceListBoxItem), new PropertyMetadata(default(KeyValuePair<string, string>)));

    public KeyValuePair<string, string> Value
    {
        get { return (KeyValuePair<string, string>)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public PluginSourceListBoxItem()
    {
        InitializeComponent();
    }
}