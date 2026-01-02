using System.Collections.Generic;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Controls.EditMode;

public class RequestAddComponentEventArgs(RoutedEvent e) : RoutedEventArgs(e)
{
    public required IList<ComponentSettings> ComponentList { get; init; }
}