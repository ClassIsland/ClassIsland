using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Assists;

/// <summary>
/// <see cref="SettingsExpander"/> 辅助类。
/// </summary>
public class SettingsExpanderAssist
{
    public static readonly AttachedProperty<bool> PreventEmptyExpansionProperty =
        AvaloniaProperty.RegisterAttached<SettingsExpanderAssist, SettingsExpander, bool>("PreventEmptyExpansion");

    public static void SetPreventEmptyExpansion(SettingsExpander obj, bool value) =>
        obj.SetValue(PreventEmptyExpansionProperty, value);

    public static bool GetPreventEmptyExpansion(SettingsExpander obj) =>
        obj.GetValue(PreventEmptyExpansionProperty);

    static SettingsExpanderAssist()
    {
        PreventEmptyExpansionProperty.Changed.AddClassHandler<SettingsExpander>(HandlePreventEmptyExpansionChanged);
    }

    private static void HandlePreventEmptyExpansionChanged(SettingsExpander expander, AvaloniaPropertyChangedEventArgs args)
    {
        expander.TemplateApplied -= ExpanderOnTemplateApplied;

        if (GetPreventEmptyExpansion(expander))
        {
            expander.TemplateApplied += ExpanderOnTemplateApplied;
        }

        UpdateExpandingHandler(expander);
    }

    private static void ExpanderOnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is SettingsExpander expander)
        {
            UpdateExpandingHandler(expander);
        }
    }

    private static void UpdateExpandingHandler(SettingsExpander expander)
    {
        var innerExpander = expander.GetTemplateChildren()
            .OfType<Expander>()
            .FirstOrDefault(x => x.Name == "Expander");

        if (innerExpander == null)
        {
            return;
        }

        innerExpander.Expanding -= InnerExpanderOnExpanding;

        if (GetPreventEmptyExpansion(expander))
        {
            innerExpander.Expanding += InnerExpanderOnExpanding;
        }
    }

    private static void InnerExpanderOnExpanding(object? sender, CancelRoutedEventArgs e)
    {
        if (sender is not Expander { TemplatedParent: SettingsExpander expander } ||
            !GetPreventEmptyExpansion(expander) ||
            expander.ItemCount != 0)
        {
            return;
        }

        e.Cancel = true;
        e.Handled = true;
    }
}
