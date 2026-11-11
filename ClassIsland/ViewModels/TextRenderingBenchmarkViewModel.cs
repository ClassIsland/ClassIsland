using System.Collections.ObjectModel;
using ClassIsland.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

internal sealed partial class TextRenderingBenchmarkViewModel : ObservableObject
{
    public string[] LengthOptions { get; } = ["短文本", "长文本"];
    public int[] CountOptions { get; } = [5, 10, 100, 500, 1000];
    public string[] RendererOptions { get; } = ["TextBlock", "Markdown"];

    [ObservableProperty] private int _lengthIndex;
    [ObservableProperty] private int _selectedCount = 10;
    [ObservableProperty] private int _rendererIndex;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _canScroll;
    [ObservableProperty] private bool _canExport;
    [ObservableProperty] private string _phase = "等待测试";
    [ObservableProperty] private string _progress = "";
    [ObservableProperty] private string _latestMetrics = "切换选项后自动测试。";

    public ObservableCollection<TextBenchmarkResult> Results { get; } = [];
    public TextBenchmarkCase SelectedCase => new(LengthIndex == 1, SelectedCount, RendererIndex == 1);

    public void SelectCase(TextBenchmarkCase testCase)
    {
        LengthIndex = testCase.IsLong ? 1 : 0;
        SelectedCount = testCase.Count;
        RendererIndex = testCase.IsMarkdown ? 1 : 0;
    }

    public void RefreshResults(TextBenchmarkSession session)
    {
        Results.Clear();
        foreach (var result in session.Results)
            Results.Add(result);
        CanExport = session.Results.Count > 0;
    }
}
