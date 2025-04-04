using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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