using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using ClassIsland.Core.Models.Notification.Templates;

namespace ClassIsland.Core.Controls.NotificationTemplates;

/// <summary>
/// RollingTextTemplate.xaml 的交互逻辑
/// </summary>
public partial class RollingTextTemplate : UserControl
{
    private RollingTextTemplateData Data { get; }

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
        var visual = ElementComposition.GetElementVisual(Description);
        if (visual == null)
        {
            return;
        }

        var compositor = visual.Compositor;
        var anim = compositor.CreateVector3DKeyFrameAnimation();
        anim.Target = nameof(visual.Offset);
        anim.Duration = Data.Duration / Math.Max(Data.RepeatCount, 1);
        anim.IterationBehavior = AnimationIterationBehavior.Forever;
        anim.InsertKeyFrame(0f, visual.Offset with { X = RootCanvas.Bounds.Width }, new LinearEasing());
        anim.InsertKeyFrame(1f, visual.Offset with { X = -Description.Bounds.Width }, new LinearEasing());
        visual.StartAnimation(nameof(visual.Offset), anim);
    }
}
