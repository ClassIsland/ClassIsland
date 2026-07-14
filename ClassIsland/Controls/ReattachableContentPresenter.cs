using Avalonia;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;

namespace ClassIsland.Controls;

/// <summary>
/// Presents a reusable control while releasing it from the visual and logical trees when unloaded.
/// </summary>
public class ReattachableContentPresenter : ContentPresenter
{
    public static readonly StyledProperty<object?> ContentSourceProperty =
        AvaloniaProperty.Register<ReattachableContentPresenter, object?>(nameof(ContentSource));

    static ReattachableContentPresenter()
    {
        ContentSourceProperty.Changed.AddClassHandler<ReattachableContentPresenter>(
            static (presenter, args) =>
                presenter.SetCurrentValue(ContentProperty, args.NewValue));
    }

    /// <summary>
    /// Gets or sets the content that will be attached while the presenter is loaded.
    /// </summary>
    public object? ContentSource
    {
        get => GetValue(ContentSourceProperty);
        set => SetValue(ContentSourceProperty, value);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        SetCurrentValue(ContentProperty, ContentSource);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        SetCurrentValue(ContentProperty, null);
        base.OnDetachedFromLogicalTree(e);
    }
}
