using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Views;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Services;
using ClassIsland.Shared;
using ReactiveUI;

namespace ClassIsland.Views;

public partial class SplashWindow : SplashWindowBase
{
    public ISplashService SplashService { get; } = IAppHost.GetService<ISplashService>();
    public SettingsService SettingsService { get; } = IAppHost.GetService<SettingsService>();

    private IDisposable? _splashStatusObserver;

    private double _lastProgress = 0;

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContext = this;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PreInitAnimations();
    }

    public override async Task StartSplash()
    {
        SplashService.ProgressChanged += SplashServiceOnProgressChanged;
        await base.StartSplash();
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Transparent | WindowFeatures.ToolWindow, true);
        _splashStatusObserver = SplashService.ObservableForProperty(x => x.SplashStatus)
            .Subscribe(_ => TryRunJobs());
        SetupIntroAnimation(AppLogo, TimeSpan.FromMilliseconds(0));
        SetupIntroAnimation(Status, TimeSpan.FromMilliseconds(150));
        await TryWaitJobs();
    }

    private async void SplashServiceOnProgressChanged(object? sender, double value)
    {
        _ = UpdateAnimationAsync(value);
        TryRunJobs();
    }

    private async Task UpdateAnimationAsync(double value, bool isFinal = false)
    {
        var visualProgressBarFill = ElementComposition.GetElementVisual(ProgressBarFill);
        if (visualProgressBarFill == null)
        {
            return;
        }

        var currentOffset = visualProgressBarFill.Offset;
        var compositor = visualProgressBarFill.Compositor;
        var progressAnimation = compositor.CreateVector3DKeyFrameAnimation();
        progressAnimation.InsertKeyFrame(1.0f, currentOffset with { X = -400 * (1 - value / 100) }, new CubicEaseOut());
        var duration = isFinal ? TimeSpan.FromSeconds(0.15) : TimeSpan.FromSeconds(Math.Max((value - _lastProgress) / 8, 0.5));
        progressAnimation.Duration = duration;
        visualProgressBarFill.StartAnimation(nameof(visualProgressBarFill.Offset), progressAnimation);
        _lastProgress = value;

        await Task.Delay(duration);
    }

    private void PreInitAnimations()
    {
        var visualStatus = ElementComposition.GetElementVisual(Status);
        if (visualStatus == null)
        {
            return;
        }
        var visualProgressBarFill = ElementComposition.GetElementVisual(ProgressBarFill);
        if (visualProgressBarFill == null)
        {
            return;
        }

        visualStatus.Opacity = 0f;
        visualProgressBarFill.Offset = visualProgressBarFill.Offset with { X = -400 };
    }

    private void SetupIntroAnimation(Visual visual, TimeSpan delay)
    {
        var compositionVisual = ElementComposition.GetElementVisual(visual);
        if (compositionVisual == null)
        {
            return;
        }
        var compositor = compositionVisual.Compositor;
        var slideAnimation = compositor.CreateVector3DKeyFrameAnimation();
        slideAnimation.InsertKeyFrame(0.0f, compositionVisual.Offset with { X = 32 });
        slideAnimation.InsertKeyFrame(1.0f, compositionVisual.Offset with { X = 0 }, new ExponentialEaseInOut());
        slideAnimation.Duration = TimeSpan.FromSeconds(0.3);
        slideAnimation.DelayTime = delay;
        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.InsertKeyFrame(0.0f, 0f);
        opacityAnimation.InsertKeyFrame(1.0f, 1f, new CubicEaseOut());
        opacityAnimation.Duration = TimeSpan.FromSeconds(0.3);
        opacityAnimation.DelayTime = delay;
        compositionVisual.StartAnimation(nameof(compositionVisual.Offset), slideAnimation);
        compositionVisual.StartAnimation(nameof(compositionVisual.Opacity), opacityAnimation);
    }

    public override async Task EndSplash()
    {
        SplashService.ProgressChanged -= SplashServiceOnProgressChanged;
        await UpdateAnimationAsync(100, true);
        await base.EndSplash();
        _splashStatusObserver?.Dispose();
        _splashStatusObserver = null;
    }
}