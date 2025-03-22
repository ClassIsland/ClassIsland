using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using ClassIsland.Views;

namespace ClassIsland.Controls;

/// <summary>
/// SplashControl.xaml 的交互逻辑
/// </summary>
public partial class SplashControl : UserControl
{
    public ISplashService SplashService { get; } = App.GetService<ISplashService>();
    public SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    public static readonly DependencyProperty CurrentProgressProperty = DependencyProperty.Register(
        nameof(CurrentProgress), typeof(double), typeof(SplashControl), new PropertyMetadata(0.0));

    private double _lastProgress = 0.0;
    private double _lastProgressDelta = 0.0;

    public double CurrentProgress
    {
        get { return (double)GetValue(CurrentProgressProperty); }
        set { SetValue(CurrentProgressProperty, value); }
    }

    public SplashControl()
    {
        InitializeComponent();
        SplashService.ProgressChanged += SplashServiceOnProgressChanged;
        SplashService.SplashEnded += SplashServiceOnSplashEnded;
    }

    private void SplashServiceOnSplashEnded(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            UpdateProgress(100, 0.1, (_, _) =>
            {
                Window.GetWindow(this)?.Close();
            });
        });
    }

    private void SplashServiceOnProgressChanged(object? sender, double e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            UpdateProgress(e, Math.Max((e - _lastProgress ) / 8, 0.5));
            _lastProgressDelta = e - _lastProgress;
            _lastProgress = e;
        });
        
    }

    private void UpdateProgress(double e, double seconds, EventHandler? callBack = null)
    {
        var da = new DoubleAnimation()
        {
            From = CurrentProgress,
            To = e,
            Duration = new Duration(TimeSpan.FromSeconds(seconds)),
            EasingFunction = new ExponentialEase()
        };
        var storyboard = new Storyboard()
        {
        };
        Storyboard.SetTarget(da, this);
        Storyboard.SetTargetProperty(da, new PropertyPath(CurrentProgressProperty));
        storyboard.Children.Add(da);
        if (callBack != null)
            storyboard.Completed += callBack;
        //storyboard.RepeatBehavior = ;
        storyboard.Begin();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && (bool)e.NewValue)
        {
            BeginStoryboard((Storyboard)FindResource("Intro"));
            Console.WriteLine("splash control visible.");
        }
        base.OnPropertyChanged(e);
    }

    protected override void OnInitialized(EventArgs e)
    {
        Console.WriteLine("splash control init.");
        base.OnInitialized(e);
    }
}