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
using ClassIsland.Core.Enums.SettingsWindow;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// DebugBrushesSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("debug_brushes", "笔刷", PackIconKind.BrushOutline, PackIconKind.Brush, SettingsPageCategory.Debug)]
public partial class DebugBrushesSettingsPage : SettingsPageBase
{
    public DebugBrushesSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }
}