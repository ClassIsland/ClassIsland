using System.Collections.Generic;
using Avalonia;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Controls.EditMode;

public class EditableComponentsListBoxEventArgs(RoutedEvent e) : RoutedEventArgs(e)
{
    public required ComponentSettings Settings { get; init; }
    public required IReadOnlyList<ComponentSettings> ComponentStack { get; init; }
    public required IList<ComponentSettings> ComponentsList { get; init; }
    public required Point ItemPosition { get; init; }
}