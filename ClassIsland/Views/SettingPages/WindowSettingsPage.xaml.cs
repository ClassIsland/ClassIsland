using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// WindowSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("window", "窗口", PackIconKind.WindowMaximize, PackIconKind.WindowMaximize, SettingsPageCategory.Internal)]
public partial class WindowSettingsPage : SettingsPageBase
{
    public SettingsService SettingsService { get; }

    public WindowSettingsViewModel ViewModel { get; } = new();

    public WindowSettingsPage(SettingsService settingsService)
    {
        InitializeComponent();
        DataContext = this;
        SettingsService = settingsService;
        ViewModel.Screens = new ObservableCollection<Screen>(Screen.AllScreens);
        var taskbarTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        taskbarTimer.Tick += TaskbarTimer_Tick;
        taskbarTimer.Start();
        TaskbarTimer_Tick();
    }

    private void ButtonRefreshMonitors_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Screens = new ObservableCollection<Screen>(Screen.AllScreens);
    }

    private void TaskbarTimer_Tick(object? _ = null, EventArgs? e = null)
    {
        var t = DateTime.Now.ToShortTimeString();
        if (DateTime.Now.Second % 2 == 0) t = t.Replace(":", " ");
        TaskbarTime.Text = t;
    }
}