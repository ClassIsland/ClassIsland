using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Shapes;
using ClassIsland.ViewModels;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Views;
/// <summary>
/// WelcomeWindow.xaml 的交互逻辑
/// </summary>
public partial class WelcomeWindow : Window
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

    protected override void OnContentRendered(EventArgs e)
    {
        //DialogResult = false;
        base.OnContentRendered(e);
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
}