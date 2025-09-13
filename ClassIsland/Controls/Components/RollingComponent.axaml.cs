using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Helpers;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Shared;

namespace ClassIsland.Controls.Components;

/// <summary>
/// RollingComponent.xaml 的交互逻辑
/// </summary>
[ContainerComponent]
[ComponentInfo("70FCD5EA-3FAE-4E06-ACA2-4F4DF47F9ACD", "轮播容器", "\uf279", "滚动显示组件内的内容。")]
public partial class RollingComponent : ComponentBase<RollingComponentSettings>
{
    public IRulesetService RulesetService { get; } = IAppHost.GetService<IRulesetService>();

    public static readonly StyledProperty<double> InnerContainerWidthProperty = AvaloniaProperty.Register<RollingComponent, double>(
        nameof(InnerContainerWidth), default(double));

    public double InnerContainerWidth
    {
        get => GetValue(InnerContainerWidthProperty);
        set => SetValue(InnerContainerWidthProperty, value);
    }

    public static readonly StyledProperty<double> ScrollWidthProperty = AvaloniaProperty.Register<RollingComponent, double>(
        nameof(ScrollWidth), default(double));

    public double ScrollWidth
    {
        get => GetValue(ScrollWidthProperty);
        set => SetValue(ScrollWidthProperty, value);
    }

    public static readonly StyledProperty<double> OuterContainerWidthProperty = AvaloniaProperty.Register<RollingComponent, double>(
        nameof(OuterContainerWidth), default(double));

    public double OuterContainerWidth
    {
        get => GetValue(OuterContainerWidthProperty);
        set => SetValue(OuterContainerWidthProperty, value);
    }

    public static readonly StyledProperty<bool> IsScrollingProperty = AvaloniaProperty.Register<RollingComponent, bool>(
        nameof(IsScrolling), default(bool));

    public bool IsScrolling
    {
        get => GetValue(IsScrollingProperty);
        set => SetValue(IsScrollingProperty, value);
    }

    private CompositionAnimation? _currentScrollingAnimation;
    private CancellationTokenSource? _animationCancellationSource;
    
    private bool _isStopRuleSatisfied = false;
    private double _pausePos = 0;

    public RollingComponent()
    {
        InitializeComponent();
    }

    private void UpdateScrollState()
    {
        IsScrolling = OuterContainerWidth < InnerContainerWidth;
        if (!IsScrolling || _isStopRuleSatisfied)
        {
            ResetScrollState();
            return;
        }

        var width = (InnerContainerWidth);
        var pausePos = _pausePos = !Settings.IsPauseEnabled || Settings.PauseOffsetX > width ? 0.0 : Settings.PauseOffsetX;
        var durationSeconds = width / Math.Max(1, Settings.SpeedPixelPerSecond);
        var pauseSeconds = Settings.IsPauseEnabled ? Settings.PauseSeconds : 0.0;
        var backSeconds = OuterContainerWidth / Math.Max(1, Settings.SpeedPixelPerSecond);
        var totalSeconds = durationSeconds + pauseSeconds + backSeconds;
        var visual = ElementComposition.GetElementVisual(GridScrollContainer);
        if (visual is null)
        {
            return;
        }

        var compositor = visual.Compositor;
        var anim = compositor.CreateVector3DKeyFrameAnimation();
        anim.Duration = TimeSpanHelper.FromSecondsSafe(pauseSeconds + durationSeconds);
        anim.IterationBehavior = AnimationIterationBehavior.Forever;
        anim.InsertKeyFrame(0f, visual.Offset with { X = 0 }, new LinearEasing());
        anim.InsertKeyFrame((float)(pauseSeconds / totalSeconds), visual.Offset with { X = -pausePos }, new LinearEasing());
        anim.InsertKeyFrame((float)((pauseSeconds + durationSeconds) / totalSeconds), visual.Offset with { X = -( width + pausePos ) },
            new LinearEasing());
        anim.InsertKeyFrame((float)((pauseSeconds + durationSeconds) / totalSeconds), visual.Offset with { X = OuterContainerWidth }, 
            new LinearEasing());
        anim.InsertKeyFrame(1f, visual.Offset with { X = 0 }, new LinearEasing());
        anim.Target = nameof(visual.Offset);

        ResetScrollState();
        if (_animationCancellationSource?.IsCancellationRequested == false)
        {
            _animationCancellationSource?.Cancel();
        }
        _animationCancellationSource = new CancellationTokenSource();
        _animationCancellationSource.Token.Register(() =>
        {
            visual.ImplicitAnimations?.Clear();
        });
        _currentScrollingAnimation = anim;
        visual.StartAnimation(nameof(visual.Offset), anim);
    }

    private void ResetScrollState()
    {
        _animationCancellationSource?.Cancel();
        _animationCancellationSource = null;
        _currentScrollingAnimation = null;
        ScrollWidth = -_pausePos;
        var visual = ElementComposition.GetElementVisual(GridScrollContainer);
        if (visual is null)
        {
            return;
        }

        visual.Offset = visual.Offset with { X = -_pausePos };
        var blankAnimation = visual.Compositor.CreateVector3DKeyFrameAnimation();
        blankAnimation.Target = nameof(visual.Offset);
        blankAnimation.Duration = TimeSpan.FromSeconds(1);
        blankAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        blankAnimation.InsertKeyFrame(0f, visual.Offset with { X = -_pausePos });
        blankAnimation.InsertKeyFrame(1f, visual.Offset with { X = -_pausePos });
        visual.StartAnimation(nameof(visual.Offset), blankAnimation);
    }

    private void OuterContainerStackPanel_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        OuterContainerWidth = OuterContainerStackPanel.Bounds.Width;
        UpdateScrollState();
    }

    private void ItemsControlCore_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InnerContainerWidth = ItemsControlCore.Bounds.Width;
        UpdateScrollState();
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        var prevStopState = _isStopRuleSatisfied;
        _isStopRuleSatisfied = Settings.StopOnRule && RulesetService.IsRulesetSatisfied(Settings.StopRule);

        if (prevStopState != _isStopRuleSatisfied)
        {
            UpdateScrollState();
        }
    }
    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Settings.PauseOffsetX) &&
            e.PropertyName != nameof(Settings.PauseSeconds) &&
            e.PropertyName != nameof(Settings.IsPauseEnabled) &&
            e.PropertyName != nameof(Settings.SpeedPixelPerSecond) &&
            e.PropertyName != nameof(Settings.StopOnRule))
        {
            return;
        }
        UpdateScrollState();
    }

    private void RollingComponent_OnLoaded(object sender, RoutedEventArgs e)
    {
        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        Settings.PropertyChanged += SettingsOnPropertyChanged;
    }


    private void RollingComponent_OnUnloaded(object sender, RoutedEventArgs e)
    {
        RulesetService.StatusUpdated -= RulesetServiceOnStatusUpdated;
        Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }
}
