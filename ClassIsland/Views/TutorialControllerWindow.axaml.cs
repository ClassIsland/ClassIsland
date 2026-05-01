using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Enums;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Shared;

namespace ClassIsland.Views;

public partial class TutorialControllerWindow : Window
{
    private const double BottomGapDip = 16;

    public ITutorialService TutorialService { get; } = IAppHost.GetService<ITutorialService>();

    private IWindowRuleService WindowRuleService { get; } = IAppHost.GetService<IWindowRuleService>();

    public TutorialControllerWindow()
    {
        DataContext = this;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }


    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdatePositionToWorkAreaBottomLeft();
        Dispatcher.UIThread.Post(UpdatePositionToWorkAreaBottomLeft, DispatcherPriority.Render);
        WindowRuleService.ForegroundWindowChanged += WindowRuleServiceOnForegroundWindowChanged;
    }

    private void WindowRuleServiceOnForegroundWindowChanged(object? sender, ForegroundWindowChangedEventArgs e)
    {
        PlatformServices.WindowPlatformService.SetWindowFeature(this, WindowFeatures.Topmost, true);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        WindowRuleService.ForegroundWindowChanged -= WindowRuleServiceOnForegroundWindowChanged;
    }

    public override void Show()
    {
        base.Show();
    }

    private void ButtonSkip_OnClick(object? sender, RoutedEventArgs e)
    {
        TutorialService.SkipTutorial();
    }

    private void UpdatePositionToWorkAreaBottomLeft()
    {
        var targetScreen = TutorialService.AttachedToplevel is Window targetWindow
            ? Screens.ScreenFromWindow(targetWindow)
            : null;
        targetScreen ??= Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (targetScreen == null)
        {
            return;
        }

        var scaling = targetScreen.Scaling;
        var bottomGapPx = (int)Math.Round(BottomGapDip * scaling);
        var windowHeightPx = (int)Math.Round(Bounds.Height * scaling);
        var x = targetScreen.WorkingArea.X;
        var y = targetScreen.WorkingArea.Bottom - windowHeightPx - bottomGapPx;

        Position = new PixelPoint(x, y);
    }
}
