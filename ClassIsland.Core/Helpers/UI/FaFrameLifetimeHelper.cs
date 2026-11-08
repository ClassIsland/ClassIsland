using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Helpers.UI;

internal static class FaFrameLifetimeHelper
{
    private static readonly ConditionalWeakTable<FAFrame, Subscription> Subscriptions = new();

    internal static void Attach(FAFrame frame) => Subscriptions.GetValue(frame, x => new Subscription(x));

    private sealed class Subscription
    {
        private readonly EventHandler<RoutedEventArgs> _backRequestedHandler;
        private TopLevel? _topLevel;

        [DynamicDependency("OnTopLevelBackRequested", typeof(FAFrame))]
        internal Subscription(FAFrame frame)
        {
            // FluentAvalonia 3.0.0 looks up the visual parent during detach, after that
            // parent has already been removed. Keep the original source for unsubscription.
            _backRequestedHandler = typeof(FAFrame)
                .GetMethod("OnTopLevelBackRequested", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<EventHandler<RoutedEventArgs>>(frame);
            _topLevel = TopLevel.GetTopLevel(frame);
            frame.AttachedToVisualTree += OnAttachedToVisualTree;
            frame.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _topLevel = TopLevel.GetTopLevel((FAFrame)sender!);
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_topLevel != null)
            {
                _topLevel.BackRequested -= _backRequestedHandler;
                _topLevel = null;
            }
        }
    }
}
