using System.Windows;
using Avalonia.Interactivity;
using ClassIsland.Controls;
using ClassIsland.Controls.TimeLine;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Models.EventArgs;

public class SeparatorLikeTimePointMovedEventArgs(TimeLayoutItem item) : RoutedEventArgs(TimeLineListItemSeparatorAdornerControl.SeparatorLikeTimePointMovedEvent)
{
    public TimeLayoutItem Item { get; } = item;
}