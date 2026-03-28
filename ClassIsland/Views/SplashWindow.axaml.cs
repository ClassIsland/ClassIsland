using System;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Services;
using ClassIsland.Shared;
using ReactiveUI;

namespace ClassIsland.Views;

public partial class SplashWindow : Window, ISplashProvider
{
    public ISplashService SplashService { get; } = IAppHost.GetService<ISplashService>();
    public SettingsService SettingsService { get; } = IAppHost.GetService<SettingsService>();

    private IDisposable? _splashStatusObserver;
    
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContext = this;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PreInitAnimations();
    }

    public void StartSplash()
    {
        Show();
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Transparent | WindowFeatures.ToolWindow, true);
        _splashStatusObserver = SplashService.ObservableForProperty(x => x.SplashStatus)
            .Subscribe(_ => TryRunJobs());
        TryRunJobs();
        SetupIntroAnimation(AppLogo, TimeSpan.FromMilliseconds(0));
        SetupIntroAnimation(Status, TimeSpan.FromMilliseconds(150));
    }

    private void PreInitAnimations()
    {
        var visualStatus = ElementComposition.GetElementVisual(Status);
        if (visualStatus == null)
        {
            return;
        }

        visualStatus.Opacity = 0f;
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
        slideAnimation.InsertKeyFrame(1.0f, compositionVisual.Offset with { X = 0 }, new CubicEaseOut());
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

    private static void TryRunJobs()
    {
        if (IThemeService.IsWaitForTransientDisabled)
        {
            return;
        }

        Dispatcher.UIThread.RunJobs();
    }

    public void EndSplash()
    {
        Close();
        _splashStatusObserver?.Dispose();
        _splashStatusObserver = null;
    }
}