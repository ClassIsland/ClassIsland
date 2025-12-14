using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 在鼠标悬停时横向展开文字说明的 <see cref="Button"/>。
/// </summary>
/// <seealso cref="Glyph"/>
/// <seealso cref="Text"/>
/// <seealso cref="IsKeepingExpanded"/>
[TemplatePart(Name = "PART_Button", Type = typeof(Button))]
[TemplatePart(Name = "PART_IconText", Type = typeof(IconText))]
[TemplatePart(Name = "PART_GhostIconOnlyIconText", Type = typeof(IconText))]
[PseudoClasses(":expanded")]
public class AnimatedIconButton : Button
{
    public AnimatedIconButton()
    {
        
    }

    public static FuncValueConverter<double, Thickness> TextBlockPaddingFuncConverter { get; } =
        new(s => new Thickness(10, 5, s, 6));

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _button = e.NameScope.Find<Button>("PART_Button");
        if (_button == null) return;

        if (_iconText != null)
        {
            _iconText.SizeChanged -= IconTextOnSizeChanged;
        }
        if (_ghostIconOnlyIconText != null)
        {
            _ghostIconOnlyIconText.SizeChanged -= IconTextOnSizeChanged;
        }

        _iconText = (_button.Content as Control)?.GetVisualChildren().OfType<IconText>().FirstOrDefault(x => x.Name == "PART_IconText");
        _ghostIconOnlyIconText = (_button.Content as Control)?.GetVisualChildren().OfType<IconText>().FirstOrDefault(x => x.Name == "PART_GhostIconOnlyIconText");

        if (_iconText != null)
            _iconText.SizeChanged += IconTextOnSizeChanged;
        if (_ghostIconOnlyIconText != null)
            _ghostIconOnlyIconText.SizeChanged += IconTextOnSizeChanged;

        _isTemplateApplied = true;
        Initialize();
    }

    private void IconTextOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        MeasureWidths();
    }

    private void Initialize()
    {
        if (!_isTemplateApplied)
        {
            return;
        }
        _isKeepingExpandedSubscribe?.Dispose();
        _isKeepingExpandedSubscribe = IsKeepingExpandedProperty.Changed.Subscribe(_ => UpdateStatus());
        MeasureWidths();

        PointerExited -= Leaved;
        PointerExited += Leaved;
        PointerEntered -= Entered;
        PointerEntered += Entered;

        UpdateStatus();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        Initialize();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        PointerExited -= Leaved;
        PointerEntered -= Entered;

        _isKeepingExpandedSubscribe?.Dispose();
        CleanupCts();
    }

    private void CleanupCts()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void MeasureWidths()
    {
        if (_iconText == null || _button == null || _ghostIconOnlyIconText == null)
        {
            return;
        }

        TargetWidth = _ghostIconOnlyIconText.Bounds.Width;
        FullWidth = _iconText.Bounds.Width;
        TargetHeight = _iconText.Bounds.Height;
        
        Duration = TimeSpan.FromMilliseconds(Math.Max((int)((FullWidth - TargetWidth) * 1 + 220), 0));
    }

    private void Entered(object? sender, PointerEventArgs e)
    {
        _isExpandedByMouse = true;
        UpdateStatus();
    }

    private void Leaved(object? sender, PointerEventArgs e)
    {
        _isExpandedByMouse = false;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        PseudoClasses.Set(":expanded", IsKeepingExpanded || _isExpandedByMouse);
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

    private TimeSpan _duration;

    public static readonly DirectProperty<AnimatedIconButton, TimeSpan> DurationProperty = AvaloniaProperty.RegisterDirect<AnimatedIconButton, TimeSpan>(
        nameof(Duration), o => o.Duration, (o, v) => o.Duration = v);

    public TimeSpan Duration
    {
        get => _duration;
        set => SetAndRaise(DurationProperty, ref _duration, value);
    }

    private double _targetHeight;

    public static readonly DirectProperty<AnimatedIconButton, double> TargetHeightProperty = AvaloniaProperty.RegisterDirect<AnimatedIconButton, double>(
        nameof(TargetHeight), o => o.TargetHeight, (o, v) => o.TargetHeight = v);

    public double TargetHeight
    {
        get => _targetHeight;
        set => SetAndRaise(TargetHeightProperty, ref _targetHeight, value);
    }

    private double _targetWidth;

    public static readonly DirectProperty<AnimatedIconButton, double> TargetWidthProperty = AvaloniaProperty.RegisterDirect<AnimatedIconButton, double>(
        nameof(TargetWidth), o => o.TargetWidth, (o, v) => o.TargetWidth = v);

    public double TargetWidth
    {
        get => _targetWidth;
        set => SetAndRaise(TargetWidthProperty, ref _targetWidth, value);
    }

    private double _fullWidth;

    public static readonly DirectProperty<AnimatedIconButton, double> FullWidthProperty = AvaloniaProperty.RegisterDirect<AnimatedIconButton, double>(
        nameof(FullWidth), o => o.FullWidth, (o, v) => o.FullWidth = v);

    public double FullWidth
    {
        get => _fullWidth;
        set => SetAndRaise(FullWidthProperty, ref _fullWidth, value);
    }

    private bool _isTemplateApplied;
    private double _iconOnlyWidth;
    private double _iconTextWidth;
    private double _lastTargetWidth;
    private Button? _button;
    private IconText? _iconText;
    private IconText? _ghostIconOnlyIconText;
    private CancellationTokenSource? _cts;
    private CancellationToken CtsToken => (_cts ??= new CancellationTokenSource()).Token;
    private IDisposable? _isKeepingExpandedSubscribe;
    private bool _isExpandedByMouse;
}