using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Controls.EditMode;

public class EditableComponentsListBox : ListBox
{

    public static readonly StyledProperty<IReadOnlyList<ComponentSettings>> ContainerComponentStackProperty = AvaloniaProperty.Register<EditableComponentsListBox, IReadOnlyList<ComponentSettings>>(
        nameof(ContainerComponentStack), []);

    public IReadOnlyList<ComponentSettings> ContainerComponentStack
    {
        get => GetValue(ContainerComponentStackProperty);
        set => SetValue(ContainerComponentStackProperty, value);
    }

    public static readonly AttachedProperty<IEnumerable?> ItemsSourceInternalProperty =
        AvaloniaProperty.RegisterAttached<EditableComponentsListBox, Control, IEnumerable?>("ItemsSourceInternal", inherits: true);

    public static void SetItemsSourceInternal(Control obj, IEnumerable? value) => obj.SetValue(ItemsSourceInternalProperty, value);
    public static IEnumerable? GetItemsSourceInternal(Control obj) => obj.GetValue(ItemsSourceInternalProperty);

    public static readonly AttachedProperty<bool> IsEditableComponentsListBoxChildProperty =
        AvaloniaProperty.RegisterAttached<EditableComponentsListBox, ListBoxItem, bool>("IsEditableComponentsListBoxChild");

    public static void SetIsEditableComponentsListBoxChild(ListBoxItem obj, bool value) => obj.SetValue(IsEditableComponentsListBoxChildProperty, value);
    public static bool GetIsEditableComponentsListBoxChild(ListBoxItem obj) => obj.GetValue(IsEditableComponentsListBoxChildProperty);
}