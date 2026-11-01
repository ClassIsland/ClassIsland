using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace ClassIsland.Core.Assists;

/// <summary>
/// 为 Viewbox 的内部容器补齐逻辑树生命周期，释放祖先样式订阅。
/// </summary>
public class ViewboxLogicalTreeAssist
{
    private static readonly ConditionalWeakTable<Viewbox, Subscription> Subscriptions = new();

    /// <summary>
    /// 是否为内部容器转发逻辑树挂载和分离通知。
    /// </summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ViewboxLogicalTreeAssist, Viewbox, bool>("IsEnabled");

    /// <inheritdoc cref="IsEnabledProperty"/>
    public static bool GetIsEnabled(Viewbox viewbox) => viewbox.GetValue(IsEnabledProperty);

    /// <inheritdoc cref="IsEnabledProperty"/>
    public static void SetIsEnabled(Viewbox viewbox, bool value) => viewbox.SetValue(IsEnabledProperty, value);

    static ViewboxLogicalTreeAssist()
    {
        IsEnabledProperty.Changed.AddClassHandler<Viewbox>((viewbox, args) =>
        {
            if (args.GetNewValue<bool>())
            {
                Subscriptions.GetValue(viewbox, x => new Subscription(x));
            }
            else if (Subscriptions.TryGetValue(viewbox, out var subscription))
            {
                subscription.Dispose();
                Subscriptions.Remove(viewbox);
            }
        });
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Viewbox _viewbox;
        private readonly ILogical _container;
        private ILogicalRoot? _root;

        internal Subscription(Viewbox viewbox)
        {
            _viewbox = viewbox;
            // Avalonia 12.1.4 sets this container's parent without adding it to
            // LogicalChildren, so normal tree notifications never reach it.
            _container = (ILogical)viewbox.GetVisualChildren().Single();
            viewbox.AttachedToLogicalTree += OnAttachedToLogicalTree;
            viewbox.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
            if (viewbox.GetLogicalAncestors().OfType<ILogicalRoot>().FirstOrDefault() is { } root)
            {
                Attach(root);
            }
        }

        private void Attach(ILogicalRoot root)
        {
            _root = root;
            _container.NotifyAttachedToLogicalTree(new LogicalTreeAttachmentEventArgs(root, _container, _viewbox));
        }

        private void OnAttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Attach(e.Root);

        private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Detach();

        private void Detach()
        {
            if (_root == null)
            {
                return;
            }

            _container.NotifyDetachedFromLogicalTree(new LogicalTreeAttachmentEventArgs(_root, _container, _viewbox));
            _root = null;
        }

        public void Dispose()
        {
            _viewbox.AttachedToLogicalTree -= OnAttachedToLogicalTree;
            _viewbox.DetachedFromLogicalTree -= OnDetachedFromLogicalTree;
            Detach();
        }
    }
}
