using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Controls;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views;
/// <summary>
/// WelcomeWindow.xaml 的交互逻辑
/// </summary>
public partial class WelcomeWindow : MyWindow
{
    public WelcomeViewModel ViewModel
    {
        get;
        set;
    } = new();

    public WelcomeWindow()
    {
        DataContext = this;
        InitializeComponent();
        var reader = new StreamReader(Application.GetResourceStream(new Uri("/Assets/License.txt", UriKind.Relative))!
            .Stream);
        ViewModel.License = reader.ReadToEnd();
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        ViewModel.MasterTabIndex = 1;
    }

    private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsExitConfirmed = true;
        DialogResult = true;
        Close();
    }

    private async void WelcomeWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (ViewModel.IsExitConfirmed)
        {
            return;
        }

        e.Cancel = true;
        var r = await DialogHost.Show(FindResource("ExitAppConfirmDialog"), ViewModel.DialogId);
        if ((bool?)r == true)
        {
            ViewModel.IsExitConfirmed = true;
            Close();
        }
    }

    private void ButtonViewHelp_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsExitConfirmed = true;
        DialogResult = true;
        Close();

        var mw = (MainWindow)Application.Current.MainWindow!;
        mw.OpenHelpsWindow();
    }

    private void HyperlinkMsAppCenter_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://learn.microsoft.com/zh-cn/appcenter/sdk/data-collected",
            UseShellExecute = true
        });
    }
}