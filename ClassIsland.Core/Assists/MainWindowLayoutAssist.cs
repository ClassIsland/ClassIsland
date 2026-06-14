using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClassIsland.Core.Assists;

/// <summary>
/// Provides main-window layout hints for components.
/// </summary>
public class MainWindowLayoutAssist
{
    public static readonly AttachedProperty<int> RequestedLineSpanProperty =
        AvaloniaProperty.RegisterAttached<MainWindowLayoutAssist, Control, int>(
            "RequestedLineSpan", 1);

    public static readonly RoutedEvent<RoutedEventArgs> RequestedLineSpanChangedEvent =
        RoutedEvent.Register<Control, RoutedEventArgs>(
            "RequestedLineSpanChanged", RoutingStrategies.Bubble);

    static MainWindowLayoutAssist()
    {
        RequestedLineSpanProperty.Changed.AddClassHandler<Control>((control, args) =>
        {
            var requested = args.NewValue is int value ? value : 1;
            var coerced = Math.Max(1, requested);
            if (coerced != requested)
            {
                control.SetCurrentValue(RequestedLineSpanProperty, coerced);
                return;
            }

            control.RaiseEvent(new RoutedEventArgs(RequestedLineSpanChangedEvent));
        });
    }

    public static void SetRequestedLineSpan(Control obj, int value) =>
        obj.SetValue(RequestedLineSpanProperty, Math.Max(1, value));

    public static int GetRequestedLineSpan(Control obj) =>
        Math.Max(1, obj.GetValue(RequestedLineSpanProperty));
}
