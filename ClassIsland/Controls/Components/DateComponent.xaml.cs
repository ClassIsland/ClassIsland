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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// DateComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("DF3F8295-21F6-482E-BADA-FA0E5F14BB66", "日期", PackIconKind.CalendarOutline, "显示今天的日期和星期。")]
public partial class DateComponent : ComponentBase
{
    public ILessonsService LessonsService { get; }

    public DateComponent(ILessonsService lessonsService)
    {
        LessonsService = lessonsService;
        InitializeComponent();
    }
}