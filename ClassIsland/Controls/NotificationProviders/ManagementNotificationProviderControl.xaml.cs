using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

using ClassIsland.Shared.Protobuf.Command;

namespace ClassIsland.Controls.NotificationProviders;

public partial class ManagementNotificationProviderControl : UserControl
{
    public bool IsOverlay { get; }
    
    public SendNotification Payload { get; }
    
    public ManagementNotificationProviderControl(bool isOverlay, SendNotification payload)
    {
        IsOverlay = isOverlay;
        Payload = payload;
        InitializeComponent();
    }

    private void ManagementNotificationProviderControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        App.GetService<MainWindow>().GetCurrentDpi(out var dpi, out _);
        var da = new DoubleAnimation()
        {
            From = -Description.ActualWidth,
            To = RootCanvas.ActualWidth,
            Duration = new Duration(TimeSpan.FromSeconds(Payload.DurationSeconds)),

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