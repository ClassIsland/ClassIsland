using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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
using System.Windows.Shapes;
using ClassIsland.Models;
using ClassIsland.ViewModels;
using static ClassIsland.NativeWindowHelper;

namespace ClassIsland.Views;

/// <summary>
/// WindowsPicker.xaml 的交互逻辑
/// </summary>
public partial class WindowsPicker : Window
{
    public WindowsPickerViewModel ViewModel { get; } = new();

    public string SelectedResult
    {
        get;
        set;
    }

    public WindowsPicker(string selected)
    {
        SelectedResult = selected;
        DataContext = this;
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        ViewModel.SelectedClassName = SelectedResult;
        base.OnInitialized(e);
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        ViewModel.IsWorking = true;
        await Task.Run(UpdateWindows);
        ViewModel.IsWorking = false;
        base.OnContentRendered(e);
    }

    private void UpdateWindows()
    {
        var w = NativeWindowHelper.GetAllWindows();
        var q = ViewModel.IsFilteredFullscreen ?
            (from i in w
            where Screen.AllScreens.Any(s => new System.Drawing.Rectangle(i.WindowRect.Left, i.WindowRect.Top,
                i.WindowRect.Right - i.WindowRect.Left, i.WindowRect.Bottom - i.WindowRect.Top).Contains(s.Bounds) &&
                                             i.IsVisible && i.ClassName != "WorkerW")
            select i)
            :
            from i in w where i.IsVisible select i;
        var c = new ObservableCollection<DesktopWindow>();
        foreach (var i in q)
        {
            if (i == null) continue;
            try
            {
                c.Add(DesktopWindow.GetWindowByHWndDetailed(i.HWnd));
            }
            catch
            {
                // ignored
            }
        }

        ViewModel.DesktopWindows = c;
    }

    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ButtonDone_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedResult = ViewModel.SelectedClassName;
        DialogResult = true;
        Close();
    }

    private async void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWorking = true;
        await Task.Run(UpdateWindows);
        ViewModel.IsWorking = false;
    }
}