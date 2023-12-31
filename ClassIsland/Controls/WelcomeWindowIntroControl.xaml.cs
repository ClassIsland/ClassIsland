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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Services;

namespace ClassIsland.Controls;

/// <summary>
/// WelcomeWindowIntroControl.xaml 的交互逻辑
/// </summary>
public partial class WelcomeWindowIntroControl : UserControl
{
    private HangService HangService { get; } = App.GetService<HangService>();


    public WelcomeWindowIntroControl()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        Foreground = new SolidColorBrush(App.GetService<ThemeService>().CurrentTheme!.Body);
        _ = Task.Run(() =>
        {
            Play("Intro");
            Thread.Sleep(4000);
            HangService.AssumeHang();
            while (HangService.IsHang)
            {
                
            }
            Play("Outro");

        });
        base.OnInitialized(e);
    }

    private void Play(string key)
    {
        Dispatcher.Invoke(() =>
        {
            BeginStoryboard((Storyboard)FindResource(key));
        });
    }
}