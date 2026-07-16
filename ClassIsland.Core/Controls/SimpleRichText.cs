using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.Text.RegularExpressions;
using ClassIsland.Core.Commands;
namespace ClassIsland.Core.Controls;

/// <summary>
/// 简单的富文本控件，支持语法：<br/>
/// - <c>[显示文本](链接网址)</c> → 超链接  <br/>
/// - <c>**加粗**</c> → 加粗  <br/>
/// - <c>@关键字</c> → 从字典取值加粗显示，未取到时加粗显示原文本  <br/>
/// - <c>\\</c> → 换行  <br/>
/// - 空行 → 以半高显示  <br/>
/// 由 DeepSeek 编写。
/// </summary>
public partial class SimpleRichText : ContentControl
{
    private readonly StackPanel _panel = new();

    public SimpleRichText() => Content = _panel;

    private static void OnPropertyChanged(SimpleRichText sender, AvaloniaPropertyChangedEventArgs _)
    {
        sender.BuildContent();
    }

    private static readonly Regex ParseRegex = CompiledRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)|\*\*([^*]+)\*\*|@(\S+)", RegexOptions.Compiled)]
    private static partial Regex CompiledRegex();

    private void BuildContent()
    {
        _panel.Children.Clear();
        var text = Text;
        if (string.IsNullOrEmpty(text))
            return;

        // 换行替换
        text = text.Replace("\\", "\n");

        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var cleanLine = line.TrimEnd('\r');
            var tb = new TextBlock
            {
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
                Foreground = Foreground,
                TextWrapping = TextWrapping,
                LineHeight = LineHeight,
                TextTrimming = TextTrimming,
                TextAlignment = TextAlignment,
            };

            if (string.IsNullOrWhiteSpace(cleanLine))
            {
                // 空行：半高显示
                tb.Text = "";
                tb.Height = LineHeight * 0.4;
            }
            else
            {
                ParseLine(tb, cleanLine);
            }

            _panel.Children.Add(tb);
        }
    }

    private void ParseLine(TextBlock tb, string line)
    {
        var matches = ParseRegex.Matches(line);
        var lastIndex = 0;

        foreach (Match match in matches)
        {
            // 匹配前的纯文本
            if (match.Index > lastIndex)
                tb.Inlines.Add(new Run { Text = line[lastIndex..match.Index] });

            if (match.Groups[1].Success) // [text](url)
            {
                var link = new HyperlinkButton
                {
                    Content = match.Groups[1].Value,
                    Command = UriNavigationCommands.UriNavigationCommand,
                    CommandParameter = match.Groups[2].Value,
                    Height = 15,
                    Margin = new(0, 0, 0, -1),
                    Padding = new(0),
                    FontSize = FontSize,
                };
                tb.Inlines.Add(new InlineUIContainer { Child = link });
            }
            else if (match.Groups[3].Success) // **bold**
            {
                tb.Inlines.Add(new Run { Text = match.Groups[3].Value, FontWeight = FontWeight.Bold });
            }
            else if (match.Groups[4].Success) // @word
            {
                var key = match.Groups[4].Value;
                var resolved = UserDictionary?.TryGetValue(key, out var val) == true ? val : key;
                tb.Inlines.Add(new Run { Text = resolved, FontWeight = FontWeight.Bold });
            }

            lastIndex = match.Index + match.Length;
        }

        // 行尾剩余的纯文本
        if (lastIndex < line.Length)
            tb.Inlines.Add(new Run { Text = line[lastIndex..] });
    }

    #region 属性

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SimpleRichText, string?>(nameof(Text));

    public static readonly StyledProperty<IDictionary<string, string>?> UserDictionaryProperty =
        AvaloniaProperty.Register<SimpleRichText, IDictionary<string, string>?>(nameof(UserDictionary));

    // 以下属性从 TextBlock 转发，保持 XAML 兼容
    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<SimpleRichText>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<SimpleRichText>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextBlock.FontWeightProperty.AddOwner<SimpleRichText>();

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<SimpleRichText>();

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        TextBlock.TextWrappingProperty.AddOwner<SimpleRichText>();

    public static readonly StyledProperty<double> LineHeightProperty =
        TextBlock.LineHeightProperty.AddOwner<SimpleRichText>();

    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        TextBlock.TextTrimmingProperty.AddOwner<SimpleRichText>();

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        TextBlock.TextAlignmentProperty.AddOwner<SimpleRichText>();

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IDictionary<string, string>? UserDictionary
    {
        get => GetValue(UserDictionaryProperty);
        set => SetValue(UserDictionaryProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    static SimpleRichText()
    {
        TextProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        UserDictionaryProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        FontFamilyProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        FontSizeProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        FontWeightProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        ForegroundProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        TextWrappingProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        LineHeightProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        TextTrimmingProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
        TextAlignmentProperty.Changed.AddClassHandler<SimpleRichText>(OnPropertyChanged);
    }

    #endregion
}
