using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace ClassIsland.Controls.EditMode;

public class EditableComponentsListBox : ListBox
{
    protected override Type StyleKeyOverride => typeof(EditableComponentsListBox);
}