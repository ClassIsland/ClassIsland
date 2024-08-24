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
using ClassIsland.Models;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// ScheduleComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("1DB2017D-E374-4BC6-9D57-0B4ADF03A6B8", "课程表", PackIconKind.Schedule, "显示当前的课程表信息。")]
public partial class ScheduleComponent
{
    public ILessonsService LessonsService { get; }

    public SettingsService SettingsService { get; }

    public IProfileService ProfileService { get; }

    public ScheduleComponent(ILessonsService lessonsService, SettingsService settingsService, IProfileService profileService)
    {
        LessonsService = lessonsService;
        SettingsService = settingsService;
        ProfileService = profileService;
        InitializeComponent();
    }
}