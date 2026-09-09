using System.Globalization;
using Avalonia.Media;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.SimpleExpression;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Icons;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Models.UI;

internal partial class IconExpressionEditorViewModel : ObservableObject
{
    private static readonly Lazy<IconDescriptor[]> FluentCatalog = new(CreateCatalog<FluentIconKind>);
    private static readonly Lazy<IconDescriptor[]> LucideCatalog = new(CreateCatalog<LucideIconKind>);
    private readonly IconExpressionEditorItem[]?[] _items = new IconExpressionEditorItem[2][];
    private bool _synchronizing;
    private bool _activated;
    private int _expressionType = -1;
    private string? _glyph;
    private IconExpressionEditorItem? _selectedItem;

    public event Action<string>? ExpressionEdited;

    [ObservableProperty] private string _imagePath = "";
    [ObservableProperty] private string _query = "";
    [ObservableProperty] private int _selectedType;
    [ObservableProperty] private FAIconSource _preview = new FluentIconSource(FluentIcons.IconsRegular);
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _isBrowsing;
    [ObservableProperty] private IReadOnlyList<IconExpressionEditorItem> _filteredIcons = [];

    public bool IsImage => SelectedType == 2;
    public bool HasNoResults => FilteredIcons.Count == 0;

    public void Activate()
    {
        _activated = true;
        RefreshIcons();
    }

    public void ApplyExpression(string? expression)
    {
        _synchronizing = true;
        try
        {
            var imagePath = "";
            _expressionType = -1;
            _glyph = null;
            var source = string.IsNullOrWhiteSpace(expression)
                ? null : IconExpressionHelper.TryParseOrNull(expression);
            Preview = source ?? new FluentIconSource(FluentIcons.IconsRegular);
            Message = !string.IsNullOrWhiteSpace(expression) && source == null
                ? "无法预览此图标，请选择其他图标。" : "";

            if (expression?.Length == 1)
            {
                _expressionType = 0;
                _glyph = expression;
            }
            else if (!string.IsNullOrWhiteSpace(expression))
            {
                try
                {
                    var parsed = SimpleExprParser.Parse(expression);
                    // Only map expressions that can be represented without dropping arguments.
                    if (parsed.Arguments.Length == 1)
                    {
                        _expressionType = parsed.FunctionName switch
                        {
                            "fluent" => 0,
                            "lucide" => 1,
                            "img" => 2,
                            _ => -1
                        };
                        if (_expressionType == 2)
                            imagePath = parsed.Arguments[0];
                        else if (_expressionType >= 0)
                            _glyph = parsed.Arguments[0];
                    }
                }
                catch (FormatException)
                {
                    // Invalid external values must remain intact until the user chooses a replacement.
                }
            }

            ImagePath = imagePath;
            if (_expressionType >= 0)
                SelectedType = _expressionType;
            UpdateSelection();
        }
        finally
        {
            _synchronizing = false;
        }
    }

    partial void OnImagePathChanged(string value)
    {
        if (!_synchronizing)
            EditExpression(string.IsNullOrWhiteSpace(value) ? "" : FormatExpression("img", value));
    }

    partial void OnSelectedTypeChanged(int value)
    {
        OnPropertyChanged(nameof(IsImage));
        RefreshIcons();
    }

    partial void OnQueryChanged(string value) => RefreshIcons();

    partial void OnFilteredIconsChanged(IReadOnlyList<IconExpressionEditorItem> value) =>
        OnPropertyChanged(nameof(HasNoResults));

    public void SelectIcon(IconExpressionEditorItem item)
    {
        EditExpression(FormatExpression(item.IsLucide ? "lucide" : "fluent", item.Glyph));
    }

    private void EditExpression(string expression)
    {
        ApplyExpression(expression);
        ExpressionEdited?.Invoke(expression);
    }

    private void RefreshIcons()
    {
        if (!_activated || SelectedType is < 0 or > 1)
        {
            FilteredIcons = [];
            return;
        }

        var items = _items[SelectedType] ??= (SelectedType == 0 ? FluentCatalog : LucideCatalog).Value
            .Select(x => new IconExpressionEditorItem(x, SelectedType == 1)).ToArray();
        var query = Query.Trim();
        var normalized = NormalizeName(query);
        var code = query;
        if (code.StartsWith("U+", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("\\u", StringComparison.OrdinalIgnoreCase))
            code = code[2..];
        var hasCode = int.TryParse(code, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint);

        FilteredIcons = query.Length == 0 ? items : items.Where(x =>
            x.Descriptor.SearchName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
            (hasCode && x.Descriptor.CodePoint == codePoint) || x.Glyph == query).ToArray();
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (_selectedItem != null)
            _selectedItem.IsSelected = false;
        _selectedItem = _expressionType is 0 or 1
            ? _items[_expressionType]?.FirstOrDefault(x => x.Glyph == _glyph) : null;
        if (_selectedItem != null)
            _selectedItem.IsSelected = true;
    }

    internal static string FormatExpression(string function, string argument) =>
        $"{function}(\"{argument.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\")";

    private static string NormalizeName(string name) =>
        string.Concat(name.Where(x => !char.IsWhiteSpace(x) && x is not '-' and not '_'));

    private static IconDescriptor[] CreateCatalog<T>() where T : struct, Enum =>
        Enum.GetNames<T>().Order(StringComparer.OrdinalIgnoreCase).Select(name =>
        {
            var codePoint = Convert.ToInt32(Enum.Parse<T>(name), CultureInfo.InvariantCulture);
            return new IconDescriptor(name, char.ConvertFromUtf32(codePoint), codePoint, NormalizeName(name));
        }).ToArray();
}

internal sealed record IconDescriptor(string Name, string Glyph, int CodePoint, string SearchName);

internal partial class IconExpressionEditorItem(IconDescriptor descriptor, bool isLucide) : ObservableObject
{
    public IconDescriptor Descriptor { get; } = descriptor;
    public bool IsLucide { get; } = isLucide;
    public string Glyph => Descriptor.Glyph;
    public string Description => $"{Descriptor.Name} · U+{Descriptor.CodePoint:X4}";
    public FontFamily FontFamily => IsLucide ? AppBase.LucideIconsFontFamily : AppBase.FluentIconsFontFamily;

    [ObservableProperty] private bool _isSelected;
}
