using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Helpers;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// RollingComponent.xaml 的交互逻辑
/// </summary>
[ContainerComponent]
[ComponentInfo("70FCD5EA-3FAE-4E06-ACA2-4F4DF47F9ACD", "滚动组件", PackIconKind.FormatTextRotationNone, "滚动显示组件内的内容。")]
public partial class RollingComponent
{
    public IRulesetService RulesetService { get; }

    public static readonly DependencyProperty InnerContainerWidthProperty = DependencyProperty.Register(
        nameof(InnerContainerWidth), typeof(double), typeof(RollingComponent), new PropertyMetadata(default(double)));

    public double InnerContainerWidth
    {
        get { return (double)GetValue(InnerContainerWidthProperty); }
        set { SetValue(InnerContainerWidthProperty, value); }
    }

    public static readonly DependencyProperty ScrollWidthProperty = DependencyProperty.Register(
        nameof(ScrollWidth), typeof(double), typeof(RollingComponent), new PropertyMetadata(default(double)));

    public double ScrollWidth
    {
        get { return (double)GetValue(ScrollWidthProperty); }
        set { SetValue(ScrollWidthProperty, value); }
    }

    public static readonly DependencyProperty OuterContainerWidthProperty = DependencyProperty.Register(
        nameof(OuterContainerWidth), typeof(double), typeof(RollingComponent), new PropertyMetadata(default(double)));

    public double OuterContainerWidth
    {
        get { return (double)GetValue(OuterContainerWidthProperty); }
        set { SetValue(OuterContainerWidthProperty, value); }
    }

    public static readonly DependencyProperty IsScrollingProperty = DependencyProperty.Register(
        nameof(IsScrolling), typeof(bool), typeof(RollingComponent), new PropertyMetadata(default(bool)));

    public bool IsScrolling
    {
        get { return (bool)GetValue(IsScrollingProperty); }
        set { SetValue(IsScrollingProperty, value); }
    }

    private Storyboard? _currentScrollingStoryboard;

    private bool _isPauseRuleSatisfied = false;
    private bool _isStopRuleSatisfied = false;
    private double _pausePos = 0;

    public RollingComponent(IRulesetService rulesetService)
    {
        RulesetService = rulesetService;
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

        var sb = new Storyboard()
        {
            RepeatBehavior = RepeatBehavior.Forever,
            FillBehavior = FillBehavior.Stop,
        };
        var width = (InnerContainerWidth + 16.0);
        var pausePos = _pausePos = !Settings.IsPauseEnabled || Settings.PauseOffsetX > width ? 0.0 : Settings.PauseOffsetX;
        var durationSeconds = width / Math.Max(1, Settings.SpeedPixelPerSecond);
        var pauseSeconds = Settings.IsPauseEnabled ? Settings.PauseSeconds : 0.0;
        var animation = new DoubleAnimationUsingKeyFrames()
        {
            KeyFrames = [
                //new LinearDoubleKeyFrame(-(width + pausePos), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(Settings.PauseSeconds + durationSeconds))),
                new LinearDoubleKeyFrame(-pausePos),
                new LinearDoubleKeyFrame(-pausePos, KeyTime.FromTimeSpan(TimeSpanHelper.FromSecondsSafe(pauseSeconds))),
                new LinearDoubleKeyFrame(-(width + pausePos), KeyTime.FromTimeSpan(TimeSpanHelper.FromSecondsSafe(pauseSeconds + durationSeconds))),

            ]
        };
        //Storyboard.SetTarget(animation, this);
        Storyboard.SetTargetProperty(animation, new PropertyPath(ScrollWidthProperty));
        sb.Children.Add(animation);

        ResetScrollState();
        _currentScrollingStoryboard = sb;
        sb.Begin(this, HandoffBehavior.SnapshotAndReplace, true);
    }

    private void ResetScrollState()
    {
        _currentScrollingStoryboard?.Stop(this);
        _currentScrollingStoryboard?.Remove(this);
        ApplyAnimationClock(ScrollWidthProperty, null);
        _currentScrollingStoryboard = null;
        ScrollWidth = -_pausePos;
    }

    private void OuterContainerStackPanel_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        OuterContainerWidth = OuterContainerStackPanel.ActualWidth;
        UpdateScrollState();
    }

    private void ItemsControlCore_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InnerContainerWidth = ItemsControlCore.ActualWidth;
        UpdateScrollState();
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        var prevPauseState = _isPauseRuleSatisfied;
        var prevStopState = _isStopRuleSatisfied;
        _isPauseRuleSatisfied = Settings.PauseOnRule && RulesetService.IsRulesetSatisfied(Settings.PauseRule);
        _isStopRuleSatisfied = Settings.StopOnRule && RulesetService.IsRulesetSatisfied(Settings.StopRule);

        if (prevStopState != _isStopRuleSatisfied)
        {
            UpdateScrollState();
        }

        if (prevPauseState == _isPauseRuleSatisfied) 
            return;
        if (_isPauseRuleSatisfied)
        {
            _currentScrollingStoryboard?.Pause(this);
        }
        else
        {
            _currentScrollingStoryboard?.Resume(this);
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