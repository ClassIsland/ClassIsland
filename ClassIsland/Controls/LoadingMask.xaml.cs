using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Models;
using ClassIsland.Services;

namespace ClassIsland.Controls;

/// <summary>
/// LoadingMask.xaml 的交互逻辑
/// </summary>
public partial class LoadingMask : UserControl
{
    private Thread CheckingThread { get; }

    private bool IsWorking { get; set; } = true;

    private async void StartProcessing()
    {
        while (IsWorking)
        {
            Thread.Sleep(50);
            if (await CheckDispatcherHangAsync(Application.Current.Dispatcher))
            {
                Dispatcher.Invoke(() =>
                {
                    Visibility = Visibility.Visible;
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    Visibility = Visibility.Collapsed;
                });
            }
        }
    }

    private ThemeService ThemeService { get; } = App.GetService<ThemeService>();

    public LoadingMask()
    {
        InitializeComponent();
        CheckingThread = new Thread(StartProcessing);
        CheckingThread.Start();
        ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;
        UpdateForeground();
    }

    private void UpdateForeground()
    {
        var isLightMode = ThemeService.CurrentRealThemeMode == 0;
        var black = Color.FromArgb(255, 48, 48, 48);
        var white = Color.FromArgb(255, 242, 242, 242);
        Dispatcher.Invoke(() =>
        {
            Foreground = isLightMode ? new SolidColorBrush(black) : new SolidColorBrush(white);
            BackgroundBorder.Background = isLightMode ? new SolidColorBrush(white) : new SolidColorBrush(black);
        });
    }

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        UpdateForeground();
    }

    ~LoadingMask()
    {
        IsWorking = false;
        CheckingThread.Interrupt();
    }

    private static async Task<bool> CheckDispatcherHangAsync(Dispatcher dispatcher)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        _ = dispatcher.InvokeAsync(() => taskCompletionSource.TrySetResult(true));
        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromMilliseconds(175)));
        // 如果任务还没完成，就是界面卡了
        return taskCompletionSource.Task.IsCompleted is false;
    }
}