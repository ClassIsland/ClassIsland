using System.Windows;
using Avalonia.Interactivity;
using ClassIsland.Controls;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Models.EventArgs;

public class SeparatorLikeTimePointMovedEventArgs(TimeLayoutItem item) : RoutedEventArgs
{
    public TimeLayoutItem Item { get; } = item;
}