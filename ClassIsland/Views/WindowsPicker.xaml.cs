using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using ClassIsland.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Core.Models;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.ViewModels;

using Microsoft.Extensions.Logging;

namespace ClassIsland.Views;

/// <summary>
/// WindowsPicker.xaml 的交互逻辑
/// </summary>
public partial class WindowsPicker : MyWindow
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
            where Screen.AllScreens.Any(s => new System.Drawing.Rectangle(i.WindowRect.left, i.WindowRect.top,
                i.WindowRect.right - i.WindowRect.left, i.WindowRect.bottom - i.WindowRect.top).Contains(s.Bounds) &&
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
            catch (Exception ex) 
            {
                App.GetService<ILogger<WindowsPicker>>().LogError(ex, "获取窗口详细信息失败：{}（{}）", i.HWnd, i.ClassName);
            }
        }

        App.GetService<IHangService>().AssumeHang();
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