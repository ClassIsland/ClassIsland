using ClassIsland.Core.Abstractions.Controls;
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
/// LegacyScheduleComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("E7831603-61A0-4180-B51B-54AD75B1A4D3", "课程表（旧）", PackIconKind.Schedule, "显示当前的课程表信息。")]
public partial class LegacyScheduleComponent : ComponentBase
{
    public LegacyScheduleComponent()
    {
        InitializeComponent();
    }
}