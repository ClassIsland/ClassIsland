using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClassIsland.Core.Controls;

internal sealed class ConfettiAdorner : Decorator
{
    internal static readonly RoutedEvent<RoutedEventArgs> ShowConfettiEvent =
        RoutedEvent.Register<ConfettiAdorner, RoutedEventArgs>("ShowConfetti", RoutingStrategies.Bubble);

    private TopLevel? _topLevel;
    private readonly ConfettiAnimation _animation = new();

    public ConfettiAdorner()
    {
        IsHitTestVisible = false;
        Child = _animation;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(ShowConfettiEvent, OnShowConfetti);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(ShowConfettiEvent, OnShowConfetti);
        _topLevel = null;

        base.OnDetachedFromVisualTree(e);
    }

    private void OnShowConfetti(object? sender, RoutedEventArgs e)
    {
        if (_animation.TryPlay())
        {
            e.Handled = true;
        }
    }
}
