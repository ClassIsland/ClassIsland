using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ClassIsland.Controls;
/// <summary>
/// StarRailLoadingControl.xaml 的交互逻辑
/// </summary>
public partial class StarRailLoadingControl : UserControl
{
    private bool _isPlayed = false;

    private void BeginStoryBoard(string key)
    {
        var sb = (Storyboard)FindResource(key);
        sb.Begin(this, true);
    }

    public StarRailLoadingControl()
    {
        InitializeComponent();
    }

    private void PART_ControlRoot_Loaded(object sender, RoutedEventArgs e)
    {

    }

    protected override async void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty)
        {
            var loop = (Storyboard)FindResource("Loop");
            if ((bool)e.NewValue)
            {
                loop.Remove();
                //loop.Seek(TimeSpan.Zero);
                
                if (!_isPlayed || loop.GetCurrentState(this) != ClockState.Active)
                {
                    BeginStoryBoard("OnLoaded");
                    await Task.Run(() => Thread.Sleep(100));
                    BeginStoryBoard("Loop");
                    _isPlayed = true;
                    //Debug.WriteLine("LOADED.");
                }

            }
            else
            {
                loop.Remove();
                //loop.Seek(TimeSpan.Zero);
                //Debug.WriteLine("Unloaded.");
            }
        }
        base.OnPropertyChanged(e);
    }

    private void Loop_OnCompleted(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }
        var sb = (Storyboard)FindResource("Loop");
        sb.Seek(TimeSpan.Zero);

        BeginStoryBoard("Loop");

    }
}

