using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Theming;

namespace ClassIsland.Controls;

/// <summary>
/// LoadingMask.xaml 的交互逻辑
/// </summary>
public partial class LoadingMask : UserControl
{
    private IThemeService ThemeService { get; } = App.GetService<IThemeService>();
    public IHangService HangService { get; } = App.GetService<IHangService>();

    private bool _isMainLoadingPlaying = false;

    public static readonly DependencyProperty IsFakeLoadingProperty = DependencyProperty.Register(
        nameof(IsFakeLoading), typeof(bool), typeof(LoadingMask), new PropertyMetadata(default(bool)));

    public bool IsFakeLoading
    {
        get { return (bool)GetValue(IsFakeLoadingProperty); }
        set { SetValue(IsFakeLoadingProperty, value); }
    }

    public LoadingMask()
    {
        InitializeComponent();
        UpdateForeground();
        ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;
    }

    private Storyboard BeginStoryboard(string key)
    {
        var sb = FindResource(key) as Storyboard ?? throw new InvalidOperationException();
        MetroProgressBar.BeginStoryboard(sb, HandoffBehavior.SnapshotAndReplace, true);
        //BeginStoryboard(sb, HandoffBehavior.SnapshotAndReplace, true);
        return sb;
    }

    private Storyboard? _fakeLoadingStoryboard;

    private Storyboard? _fakeLoadingStoppingStoryboard;

    public void StartFakeLoading()
    {
        Dispatcher.Invoke(() =>
        {
            IsFakeLoading = true;
            _isMainLoadingPlaying = true;
            _fakeLoadingStoryboard = BeginStoryboard("FakeLoading");
        });
    }

    public void FinishFakeLoading()
    {
        Dispatcher.Invoke(() =>
        {
            _fakeLoadingStoryboard?.Stop();
            _isMainLoadingPlaying = false;
            var daValue = new DoubleAnimation(MetroProgressBar.Value, 100, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new SineEase()
            };
            var daOpacity = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new SineEase()
            };
            var stopSb = _fakeLoadingStoppingStoryboard = new Storyboard()
            {
                FillBehavior = FillBehavior.Stop,
            };
            Storyboard.SetTarget(daValue, MetroProgressBar);
            Storyboard.SetTargetProperty(daValue, new PropertyPath(RangeBase.ValueProperty));
            Storyboard.SetTarget(daOpacity, MetroProgressBar);
            Storyboard.SetTargetProperty(daOpacity, new PropertyPath(OpacityProperty));
            stopSb.Children.Add(daValue);
            stopSb.Children.Add(daOpacity);
            stopSb.Completed += (sender, args) =>
            {
                stopSb.Remove();
                MetroProgressBar.Opacity = 1;
                MetroProgressBar.Value = 0;
                if (!_isMainLoadingPlaying)
                {
                    IsFakeLoading = false;
                }
            };
            stopSb.Begin();
        });
    }

    private void UpdateForeground()
    {
        var isLightMode = ThemeService.CurrentRealThemeMode == 0;
        var black = Color.FromArgb(255, 48, 48, 48);
        var white = Color.FromArgb(255, 242, 242, 242);
        var primary =
            (ThemeService.CurrentRealThemeMode == 0
                ? ThemeService.CurrentTheme?.PrimaryDark.Color
                : ThemeService.CurrentTheme?.PrimaryLight.Color) ?? Colors.DodgerBlue;
        Dispatcher.Invoke(() =>
        {
            MetroProgressBar.Foreground = new SolidColorBrush(primary);
        });
    }

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        UpdateForeground();
    }
}