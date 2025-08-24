using System.Diagnostics;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Core.Models.Notification.Templates;

namespace ClassIsland.Core.Controls.NotificationTemplates;

/// <summary>
/// RollingTextTemplate.xaml 的交互逻辑
/// </summary>
public partial class RollingTextTemplate : UserControl
{
    private RollingTextTemplateData Data { get; }

    private Stopwatch AnimationDurationStopwatch { get; } = new();

    /// <summary>
    /// 初始化一个 <see cref="RollingTextTemplate"/> 对象。
    /// </summary>
    public RollingTextTemplate(RollingTextTemplateData data)
    {
        DataContext = Data = data;
        InitializeComponent();
    }

    private void RollingTextTemplate_OnLoaded(object sender, RoutedEventArgs e)
    {
        SetupAnimation();
        AnimationDurationStopwatch.Start();
    }

    private void SetupAnimation()
    {
        var visual = ElementComposition.GetElementVisual(Description);
        if (visual == null || Data.Duration <= TimeSpan.Zero)
        {
            return;
        }

        var compositor = visual.Compositor;
        var anim = compositor.CreateVector3DKeyFrameAnimation();
        anim.Target = nameof(visual.Offset);
        var durationOnce = Data.Duration / Math.Max(Data.RepeatCount, 1);
        var completedPercent = (AnimationDurationStopwatch.Elapsed -
                                (int)(AnimationDurationStopwatch.Elapsed / durationOnce) * durationOnce) / durationOnce;
        var width = RootCanvas.Bounds.Width + Description.Bounds.Width;
        anim.Duration = durationOnce;
        anim.IterationBehavior = AnimationIterationBehavior.Forever;
        anim.InsertKeyFrame(0f, visual.Offset with { X = RootCanvas.Bounds.Width - completedPercent * width }, new LinearEasing());
        anim.InsertKeyFrame((float)(1 - completedPercent), visual.Offset with { X = -Description.Bounds.Width }, new LinearEasing());
        anim.InsertKeyFrame((float)(1 - completedPercent), visual.Offset with { X = RootCanvas.Bounds.Width }, new LinearEasing());
        anim.InsertKeyFrame(1f, visual.Offset with { X = RootCanvas.Bounds.Width - completedPercent * width }, new LinearEasing());
        
        visual.StartAnimation(nameof(visual.Offset), anim);
    }

    private void RootCanvas_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        AnimationDurationStopwatch.Stop();
        AnimationDurationStopwatch.Reset();
    }

    private void Description_OnLayoutUpdated(object? sender, EventArgs e)
    {
        // 在上级元素大小更变后 CompositionAnimation 位移动画会失效，需要重新设置。
        // DispatcherTimer.RunOnce(SetupAnimation, TimeSpan.FromSeconds(1));
        SetupAnimation();
    }
}
