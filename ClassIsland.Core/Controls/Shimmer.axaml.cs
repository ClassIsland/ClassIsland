using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 用于指示内容物的加载状态的控件。
/// </summary>
[TemplatePart(MainBorderName, typeof(Border))]
public class Shimmer : TemplatedControl
{
    private const string MainBorderName = "PART_MainBorder";
    
    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<Shimmer, object?>(
        nameof(Content));

    public static readonly StyledProperty<bool> IsContentLoadedProperty = AvaloniaProperty.Register<Shimmer, bool>(
        nameof(IsContentLoaded));

    public bool IsContentLoaded
    {
        get => GetValue(IsContentLoadedProperty);
        set => SetValue(IsContentLoadedProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly StyledProperty<bool> AutoDetectContentLoadStateProperty = AvaloniaProperty.Register<Shimmer, bool>(
        nameof(AutoDetectContentLoadState), true);

    public bool AutoDetectContentLoadState
    {
        get => GetValue(AutoDetectContentLoadStateProperty);
        set => SetValue(AutoDetectContentLoadStateProperty, value);
    }

    private Border? _mainBorder;

    private void InitAnimation(Border border)
    {
        var visual = ElementComposition.GetElementVisual(border);
        if (visual == null)
        {
            return;
        }

        var compositor = visual.Compositor;
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.Target = nameof(visual.Opacity);
        anim.Duration = TimeSpan.FromSeconds(2);
        anim.IterationBehavior = AnimationIterationBehavior.Forever;
        anim.InsertKeyFrame(0f, 0.5f);
        anim.InsertKeyFrame(0.5f, 0.8f, Easing.Parse("0.80, 0.00, 0.40, 1.00"));
        anim.InsertKeyFrame(1f, 0.5f, Easing.Parse("0.60, 0.00, 0.20, 1.00"));
        visual.StartAnimation(nameof(visual.Opacity), anim);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (this.GetTemplateChildren().OfType<Border>().FirstOrDefault(x => x.Name == MainBorderName) is {} border)
        {
            _mainBorder = border;
            _mainBorder.Loaded += MainBorderOnLoaded;
        }

        if (Content is Control control && AutoDetectContentLoadState)
        {
            if (!control.IsLoaded)
            {
                control.Loaded += ControlOnLoaded;
            }
            else
            {
                IsContentLoaded = true;
            }
        }
        base.OnApplyTemplate(e);
    }

    private void MainBorderOnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_mainBorder != null) InitAnimation(_mainBorder);
    }

    private void ControlOnLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        IsContentLoaded = true;
        control.Loaded -= ControlOnLoaded;
    }
}