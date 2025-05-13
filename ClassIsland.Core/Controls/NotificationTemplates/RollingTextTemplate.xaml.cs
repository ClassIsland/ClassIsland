using System;
using System.Collections.Generic;
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
        var da = new DoubleAnimation()
        {
            From = -Description.ActualWidth,
            To = RootCanvas.ActualWidth,
            Duration = Data.Duration / Math.Min(Data.RepeatCount, 1),
        };
        var storyboard = new Storyboard()
        {
        };
        Storyboard.SetTarget(da, Description);
        Storyboard.SetTargetProperty(da, new PropertyPath(Canvas.RightProperty));
        storyboard.Children.Add(da);
        storyboard.RepeatBehavior = RepeatBehavior.Forever;
        storyboard.Begin();
    }
}