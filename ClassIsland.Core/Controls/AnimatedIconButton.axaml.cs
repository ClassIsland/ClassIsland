using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
namespace ClassIsland.Core.Controls;

/// <summary>
/// 在鼠标悬停时横向展开文字说明的 <see cref="Button"/>。
/// </summary>
/// <seealso cref="Glyph"/>
/// <seealso cref="Text"/>
/// <seealso cref="IsKeepingExpanded"/>
[TemplatePart(Name = "PART_Button", Type = typeof(Button))]
[TemplatePart(Name = "PART_IconText", Type = typeof(IconText))]
public class AnimatedIconButton : Button
{
    public AnimatedIconButton()
    {
        _isKeepingExpandedSubscribe = IsKeepingExpandedProperty.Changed.Subscribe(_ => UpdateStatus());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _button = e.NameScope.Find<Button>("PART_Button");
        if (_button == null) return;

        _iconText = (IconText)_button.Content;

        MeasureWidths();

        PointerExited -= Leaved;
        PointerExited += Leaved;
        PointerEntered -= Entered;
        PointerEntered += Entered;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        PointerExited -= Leaved;
        PointerEntered -= Entered;

        _isKeepingExpandedSubscribe?.Dispose();
        CleanupCts();
    }

    void CleanupCts()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    void MeasureWidths()
    {
        _iconText.Text = string.Empty;
        _button.Measure(Size.Infinity);
        _targetWidth = _iconOnlyWidth = _button.DesiredSize.Width;

        _iconText.Text = Text;
        _button.Measure(new Size(10_000, 10_000));
        _iconTextWidth = _button.DesiredSize.Width;

        _button.Width = _lastTargetWidth =
            IsKeepingExpanded ? _iconTextWidth : _iconOnlyWidth;

        _duration = (int)((_iconTextWidth - _iconOnlyWidth) * 1 + 220);
    }

    void Entered(object? sender, PointerEventArgs e)
    {
        _targetWidth = _iconTextWidth;
        UpdateStatus();
    }

    void Leaved(object? sender, PointerEventArgs e)
    {
        _targetWidth = _iconOnlyWidth;
        UpdateStatus();
    }

    void UpdateStatus()
    {
        AnimateTo(IsKeepingExpanded ? _iconTextWidth : _targetWidth);
    }

    void AnimateTo(double targetWidth)
    {
        if (_lastTargetWidth == targetWidth) return;
        CleanupCts();
        _lastTargetWidth = targetWidth;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(_duration),
            Easing = new SineEaseOut(),
            FillMode = FillMode.Forward,
            Children = {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(WidthProperty, _button.Bounds.Width) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(WidthProperty, targetWidth) }
                }
            }
        };
        animation.RunAsync(_button, CtsToken);
    }



    protected override Type StyleKeyOverride => typeof(AnimatedIconButton);

    public static readonly StyledProperty<string> GlyphProperty =
        AvaloniaProperty.Register<AnimatedIconButton, string>(nameof(Glyph));

    /// 图标。形如 &#xE88D; 的 FluentIcon Glyph 格式。
    public string Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<AnimatedIconButton, string>(nameof(Text));

    /// 文本。
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<bool> IsKeepingExpandedProperty =
        AvaloniaProperty.Register<AnimatedIconButton, bool>(nameof(IsKeepingExpanded), false);

    /// 是否要保持展开/是否不允许收回。
    public bool IsKeepingExpanded
    {
        get => GetValue(IsKeepingExpandedProperty);
        set => SetValue(IsKeepingExpandedProperty, value);
    }

    double _iconOnlyWidth;
    double _iconTextWidth;
    double _targetWidth;
    double _lastTargetWidth;
    int _duration;
    Button _button;
    IconText _iconText;
    CancellationTokenSource? _cts;
    CancellationToken CtsToken => (_cts ??= new CancellationTokenSource()).Token;
    IDisposable? _isKeepingExpandedSubscribe;
}