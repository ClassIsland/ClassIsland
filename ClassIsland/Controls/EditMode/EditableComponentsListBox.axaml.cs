using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace ClassIsland.Controls.EditMode;

public partial class EditableComponentsListBox : ListBox
{
    public IReadOnlyList<ComponentInfo> ContainerComponents { get; } =
        ComponentRegistryService.Registered.Where(x => x.IsComponentContainer).ToList();

    public static readonly StyledProperty<IReadOnlyList<ComponentSettings>> ContainerComponentStackProperty = AvaloniaProperty.Register<EditableComponentsListBox, IReadOnlyList<ComponentSettings>>(
        nameof(ContainerComponentStack), []);

    public IReadOnlyList<ComponentSettings> ContainerComponentStack
    {
        get => GetValue(ContainerComponentStackProperty);
        set => SetValue(ContainerComponentStackProperty, value);
    }

    public static readonly StyledProperty<ICommand?> ShowComponentSettingsCommandProperty = AvaloniaProperty.Register<EditableComponentsListBox, ICommand?>(
        nameof(ShowComponentSettingsCommand));

    public ICommand? ShowComponentSettingsCommand
    {
        get => GetValue(ShowComponentSettingsCommandProperty);
        set => SetValue(ShowComponentSettingsCommandProperty, value);
    }
    
    public static readonly RoutedEvent<EditableComponentsListBoxEventArgs> RequestOpenChildComponentsEvent =
        RoutedEvent.Register<EditableComponentsListBoxEventArgs>(
            nameof(OpenChildComponents), RoutingStrategies.Bubble, typeof(EditableComponentsListBox));
     
    public event EventHandler<EditableComponentsListBoxEventArgs> RequestOpenChildComponents
    {
        add => AddHandler(RequestOpenChildComponentsEvent, value);
        remove => RemoveHandler(RequestOpenChildComponentsEvent, value);
    }

    public static readonly AttachedProperty<IEnumerable?> ItemsSourceInternalProperty =
        AvaloniaProperty.RegisterAttached<EditableComponentsListBox, Control, IEnumerable?>("ItemsSourceInternal", inherits: true);

    public static void SetItemsSourceInternal(Control obj, IEnumerable? value) => obj.SetValue(ItemsSourceInternalProperty, value);
    public static IEnumerable? GetItemsSourceInternal(Control obj) => obj.GetValue(ItemsSourceInternalProperty);

    public static readonly AttachedProperty<bool> IsEditableComponentsListBoxChildProperty =
        AvaloniaProperty.RegisterAttached<EditableComponentsListBox, ListBoxItem, bool>("IsEditableComponentsListBoxChild");

    public static void SetIsEditableComponentsListBoxChild(ListBoxItem obj, bool value) => obj.SetValue(IsEditableComponentsListBoxChildProperty, value);
    public static bool GetIsEditableComponentsListBoxChild(ListBoxItem obj) => obj.GetValue(IsEditableComponentsListBoxChildProperty);

    [RelayCommand]
    private void OpenChildComponents(ComponentSettings? componentSettings)
    {
        if (componentSettings == null
            || ItemsSource is not IList<ComponentSettings> components)
        {
            return;
        }

        var container = ContainerFromItem(componentSettings);
        if (container == null)
        {
            return;
        }
        var window = TopLevel.GetTopLevel(this);

        if (window == null)
            return;
        var transform = container.TransformToVisual(window);
        var pointInWindow = transform?.Transform(new Point(0, 0));
        if (pointInWindow == null)
        {
            return;
        }
        RaiseEvent(new EditableComponentsListBoxEventArgs(RequestOpenChildComponentsEvent)
        {
            Settings = componentSettings,
            ComponentStack = ContainerComponentStack,
            ComponentsList = components,
            ItemPosition = pointInWindow.Value
        });
    }

    [RelayCommand]
    private void DeleteComponent(ComponentSettings? componentSettings)
    {
        if (componentSettings == null
            || ItemsSource is not IList<ComponentSettings> components)
        {
            return;
        }

        components.Remove(componentSettings);
    }
    
    [RelayCommand]
    private void CreateContainerComponent(ComponentInfo container)
    {
        if (SelectedItem is not ComponentSettings selected)
        {
            return;
        }

        if (ItemsSource is not IList<ComponentSettings> list)
        {
            return;
        }
        var index = list.IndexOf(selected);

        if (index == -1)
        {
            return;
        }

        index = Math.Min(list.Count - 1, index);
        var newComp = new ComponentSettings()
        {
            Id = container.Guid.ToString(),
        };
        if (container.ComponentType?.BaseType != null)
        {
            newComp.Settings =
                Services.ComponentsService.LoadComponentSettings(newComp, container.ComponentType.BaseType);
        }
        list.Insert(index, newComp);
        list.Remove(selected);
        newComp.Children?.Add(selected);
        SelectedItem = newComp;
        OpenChildComponents(newComp);
    }
    
    [RelayCommand]
    private void DuplicateComponent(ComponentSettings settings)
    {
        if (ItemsSource is not IList<ComponentSettings> list)
        {
            return;
        }
        var index = list.IndexOf(settings);
        if (index == -1)
        {
            return;
        }
        index = Math.Min(list.Count - 1, index);

        var newSettings = ConfigureFileHelper.CopyObject(settings);
        list.Insert(index, newSettings);
        SelectedItem = newSettings;
    }
}