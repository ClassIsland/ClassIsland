using Avalonia;
using Avalonia.Controls.Primitives;

namespace ClassIsland.Core.Controls;

internal sealed class MultipleDateSelectorDayState : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsSelectedProperty =
        AvaloniaProperty.RegisterAttached<MultipleDateSelectorDayState, CalendarDayButton, bool>("IsSelected");

    public static bool GetIsSelected(CalendarDayButton button) => button.GetValue(IsSelectedProperty);

    public static void SetIsSelected(CalendarDayButton button, bool value) => button.SetValue(IsSelectedProperty, value);

    static MultipleDateSelectorDayState()
    {
        IsSelectedProperty.Changed.AddClassHandler<CalendarDayButton>((button, change) =>
            button.Classes.Set("date-selected", change.GetNewValue<bool>()));
    }
}
