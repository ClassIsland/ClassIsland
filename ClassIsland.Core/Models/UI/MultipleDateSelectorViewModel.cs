using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.UI;

internal partial class MultipleDateSelectorViewModel : ObservableObject
{
    [ObservableProperty] private IReadOnlySet<DateOnly> _dates = new HashSet<DateOnly>();
    [ObservableProperty] private string _preview = "";
    [ObservableProperty] private string _description = "选择日期";

    private string[] _dateLabels = [];

    public void UpdateDates(IEnumerable<DateOnly>? dates, string placeholder)
    {
        Dates = dates?.ToHashSet() ?? [];
        _dateLabels = Dates.OrderBy(x => x)
            .Select(x => x.ToString("yyyy/M/d", CultureInfo.InvariantCulture)).ToArray();
        Description = _dateLabels.Length == 0
            ? placeholder
            : $"已选择 {_dateLabels.Length} 个日期：{string.Join(", ", _dateLabels)}";
    }

    public string OverflowLabel => $"…({_dateLabels.Length})";

    public void UpdatePreview(double availableWidth, Func<string, double> measure, string placeholder)
    {
        if (_dateLabels.Length == 0)
        {
            Preview = placeholder;
            return;
        }

        var fullPreview = string.Join(", ", _dateLabels);
        if (measure(fullPreview) <= availableWidth)
        {
            Preview = fullPreview;
            return;
        }

        var prefix = "";
        var preview = OverflowLabel;
        foreach (var label in _dateLabels)
        {
            var candidatePrefix = prefix.Length == 0 ? label : $"{prefix}, {label}";
            var candidate = $"{candidatePrefix}, {OverflowLabel}";
            if (measure(candidate) > availableWidth)
                break;
            prefix = candidatePrefix;
            preview = candidate;
        }
        Preview = preview;
    }
}
