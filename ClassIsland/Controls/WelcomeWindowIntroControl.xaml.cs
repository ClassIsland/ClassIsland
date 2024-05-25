using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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