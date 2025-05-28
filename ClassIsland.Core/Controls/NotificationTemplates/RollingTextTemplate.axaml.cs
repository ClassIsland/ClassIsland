using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
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
        var anim = new Animation()
        {
            Duration = Data.Duration / Math.Min(Data.RepeatCount, 1),
            IterationCount = IterationCount.Infinite
        };
        anim.Children.Add(new KeyFrame()
        {
            Cue = new Cue(0),
            Setters =
            {
                new Setter(Canvas.RightProperty, -Description.Bounds.Width)
            }
        });
        anim.Children.Add(new KeyFrame()
        {
            Cue = new Cue(1),
            Setters =
            {
                new Setter(Canvas.RightProperty, RootCanvas.Bounds.Width)
            }
        });
        anim.RunAsync(RootCanvas);
    }
}
