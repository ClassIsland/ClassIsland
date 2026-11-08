using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// ComboBox 的抽屉选择辅助属性。
/// </summary>
public class ComboBoxAssist
{
    /// <summary>
    /// 是否在视图宿主的竖直抽屉中显示选项。
    /// </summary>
    public static readonly AttachedProperty<bool> UseDrawerProperty =
        AvaloniaProperty.RegisterAttached<ComboBoxAssist, ComboBox, bool>("UseDrawer");

    private static readonly AttachedProperty<ComboBoxDrawerPage?> DrawerPageProperty =
        AvaloniaProperty.RegisterAttached<ComboBoxAssist, ComboBox, ComboBoxDrawerPage?>("DrawerPage");

    /// <inheritdoc cref="UseDrawerProperty"/>
    public static bool GetUseDrawer(ComboBox control) => control.GetValue(UseDrawerProperty);

    /// <inheritdoc cref="UseDrawerProperty"/>
    public static void SetUseDrawer(ComboBox control, bool value) => control.SetValue(UseDrawerProperty, value);

    static ComboBoxAssist()
    {
        ComboBox.IsDropDownOpenProperty.Changed.AddClassHandler<ComboBox>(OnDropDownOpenChanged);
        UseDrawerProperty.Changed.AddClassHandler<ComboBox>((control, _) =>
        {
            if (!GetUseDrawer(control))
            {
                control.GetValue(DrawerPageProperty)?.RequestClose();
            }
        });
    }

    private static void OnDropDownOpenChanged(ComboBox control, AvaloniaPropertyChangedEventArgs args)
    {
        if (control.GetValue(DrawerPageProperty) is { } currentPage)
        {
            if (!control.IsDropDownOpen)
            {
                currentPage.RequestClose();
            }
            else if (currentPage.IsClosing)
            {
                control.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
            }
            return;
        }

        if (!control.IsDropDownOpen || !GetUseDrawer(control) ||
            control.FindAncestorOfType<ViewBase>() is not { AssociatedViewHost: { } host } owner)
        {
            return;
        }

        control.ApplyTemplate();
        var popup = control.GetTemplateChildren().OfType<Popup>()
            .FirstOrDefault(x => x.Name == "PART_Popup" && x.TemplatedParent == control);
        var contentHost = popup?.Child?.GetLogicalDescendants().OfType<Border>()
            .FirstOrDefault(x => x.Name == "PopupBorder");
        if (popup == null || contentHost?.Child is not ScrollViewer { Content: ItemsPresenter presenter } scrollViewer)
        {
            return;
        }

        var page = new ComboBoxDrawerPage(control, owner, host, popup, contentHost, scrollViewer, presenter)
        {
            Header = GetDrawerTitle(control)
        };
        control.SetValue(DrawerPageProperty, page);
        page.Open();
    }

    private static string? GetDrawerTitle(ComboBox control)
    {
        var label = control.FindAncestorOfType<Field>()?.Label;
        if (!string.IsNullOrEmpty(label))
        {
            return label;
        }

        return control.GetVisualAncestors().Select(ancestor => ancestor switch
        {
            FASettingsExpanderItem { Content: string content } => content,
            FASettingsExpander { Header: string header } => header,
            _ => null
        }).FirstOrDefault(title => !string.IsNullOrEmpty(title));
    }

    internal static void ReleasePage(ComboBox control, ComboBoxDrawerPage page)
    {
        if (ReferenceEquals(control.GetValue(DrawerPageProperty), page))
        {
            control.ClearValue(DrawerPageProperty);
        }
    }
}
