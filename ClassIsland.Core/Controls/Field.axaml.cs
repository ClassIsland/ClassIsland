using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace ClassIsland.Core.Controls;

[PseudoClasses(":has-label", ":has-prefix", ":has-suffix")]
public class Field : TemplatedControl
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<Field, string>(
        nameof(Label));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<string> SuffixProperty = AvaloniaProperty.Register<Field, string>(
        nameof(Suffix));

    public string Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public static readonly StyledProperty<string> PrefixProperty = AvaloniaProperty.Register<Field, string>(
        nameof(Prefix));

    public string Prefix
    {
        get => GetValue(PrefixProperty);
        set => SetValue(PrefixProperty, value);
    }

    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<Field, object?>(
        nameof(Content));

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public Field()
    {
        this.GetObservable(LabelProperty).Subscribe(_ => PseudoClasses.Set(":has-label", !string.IsNullOrEmpty(Label)));
        this.GetObservable(SuffixProperty).Subscribe(_ => PseudoClasses.Set(":has-suffix", !string.IsNullOrEmpty(Suffix)));
        this.GetObservable(PrefixProperty).Subscribe(_ => PseudoClasses.Set(":has-prefix", !string.IsNullOrEmpty(Prefix)));
    }
}