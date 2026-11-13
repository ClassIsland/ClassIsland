using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ClassIsland.Models;

internal sealed record TextBenchmarkCase(bool IsLong, int Count, bool IsMarkdown)
{
    public string Name => $"{(IsLong ? "长文本" : "短文本")} / {Count} / {(IsMarkdown ? "Markdown" : "TextBlock")}";
}

internal readonly record struct TextBenchmarkMemory(long HeapBytes, long AllocatedBytes, long? WorkingSetBytes,
    int Gen0, int Gen1, int Gen2)
{
    public static TextBenchmarkMemory Capture(Process process)
    {
        var heap = GC.GetTotalMemory(false);
        var allocated = GC.GetTotalAllocatedBytes(true);
        long? workingSet;
        try
        {
            process.Refresh();
            workingSet = process.WorkingSet64;
        }
        catch (PlatformNotSupportedException)
        {
            workingSet = null;
        }
        return new TextBenchmarkMemory(heap, allocated, workingSet,
            GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
    }

    public TextBenchmarkMemory Delta(TextBenchmarkMemory baseline) => new(
        HeapBytes - baseline.HeapBytes, AllocatedBytes - baseline.AllocatedBytes,
        WorkingSetBytes - baseline.WorkingSetBytes,
        Gen0 - baseline.Gen0, Gen1 - baseline.Gen1, Gen2 - baseline.Gen2);
}

internal sealed record TextBenchmarkRenderSample(int Iteration, double CreationMs, double FirstFrameMs,
    TextBenchmarkMemory Memory);

internal sealed class TextBenchmarkScrollResult
{
    public string Status { get; set; } = "不适用：内容无需滚动";
    public double DurationSeconds { get; set; }
    public int CallbackCount { get; set; }
    public double CallbackHz => DurationSeconds > 0 ? CallbackCount / DurationSeconds : 0;
    public double[] IntervalsMs { get; set; } = [];
    public double P50Ms => TextBenchmarkStatistics.Percentile(IntervalsMs, 0.50);
    public double P95Ms => TextBenchmarkStatistics.Percentile(IntervalsMs, 0.95);
    public double P99Ms => TextBenchmarkStatistics.Percentile(IntervalsMs, 0.99);
    public double MaxMs => IntervalsMs.Length > 0 ? IntervalsMs.Max() : 0;
    public double Over16MsRatio => RatioOver(16.67);
    public double Over33MsRatio => RatioOver(33.33);
    public double Over50MsRatio => RatioOver(50);
    public double DistanceDip { get; set; }
    public double MaxOffsetDip { get; set; }
    public double ScrollRangeDip { get; set; }
    public double CoverageRatio => ScrollRangeDip > 0 ? MaxOffsetDip / ScrollRangeDip : 0;
    public TextBenchmarkMemory Memory { get; set; }

    private double RatioOver(double threshold) => IntervalsMs.Length > 0
        ? (double)IntervalsMs.Count(x => x > threshold) / IntervalsMs.Length : 0;
}

internal sealed class TextBenchmarkResult(TextBenchmarkCase testCase)
{
    public TextBenchmarkCase Case { get; } = testCase;
    public string Name => Case.Name;
    public List<TextBenchmarkRenderSample> Samples { get; } = [];
    public TextBenchmarkScrollResult? Scroll { get; set; }
    public string Status { get; set; } = "进行中";
    public string? Error { get; set; }
    public double? MedianFirstFrameMs => Samples.Count == 0 ? null :
        TextBenchmarkStatistics.Median(Samples.Select(x => x.FirstFrameMs));
    public string RenderSummary => Samples.Count == 0 ? "—" :
        $"{MedianFirstFrameMs:F2} ms ({Samples.Min(x => x.FirstFrameMs):F2}–{Samples.Max(x => x.FirstFrameMs):F2})";
    public string CreationSummary => Samples.Count == 0 ? "—" :
        $"{TextBenchmarkStatistics.Median(Samples.Select(x => x.CreationMs)):F2} ms";
    public string MemorySummary => Samples.Count == 0 ? "—" :
        $"堆 {TextBenchmarkStatistics.Median(Samples.Select(x => (double)x.Memory.HeapBytes)) / 1048576:+0.00;-0.00;0.00} MiB；" +
        $"分配 {TextBenchmarkStatistics.Median(Samples.Select(x => (double)x.Memory.AllocatedBytes)) / 1048576:F2} MiB";
    public string ScrollSummary => Scroll == null ? "未测试" : Scroll.Status != "完成" ? Scroll.Status :
        $"{Scroll.CallbackHz:F1} Hz；P95 {Scroll.P95Ms:F2} ms；覆盖 {Scroll.CoverageRatio:P1}";
}

internal sealed class TextBenchmarkSession
{
    public int FormatVersion => 1;
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;
    public DateTimeOffset? FinishedAt { get; set; }
    public bool IsMatrix { get; init; }
    public string Status { get; set; } = "进行中";
    public string? InterruptionReason { get; set; }
    public Dictionary<string, string> Environment { get; init; } = [];
    public int WarmupCount => IsMatrix ? 1 : 0;
    public int SamplesPerCase => IsMatrix ? 3 : 1;
    public int ScrollSeconds => 10;
    public int ScrollSpeedDipPerSecond => 600;
    public bool ForceGcBeforeRender => true;
    public string ShortSource => TextBenchmarkCorpus.Short;
    public string LongSource => TextBenchmarkCorpus.Long;
    public List<TextBenchmarkCase> PlannedCases { get; init; } = [];
    public List<TextBenchmarkResult> Results { get; } = [];
}

internal static class TextBenchmarkCorpus
{
    public static readonly string Short = """
        ## 文本渲染 Benchmark

        今天的课程即将开始。ClassIsland helps you follow your daily schedule.
        这是一段包含 **加粗文字**、*强调内容* 和 `inline code` 的固定测试文本。

        请准备课本，保持专注。The quick brown fox jumps over the lazy dog. 0123456789.
        """.Replace("\r\n", "\n") + "\n\n";

    public static readonly string Long = string.Concat(Enumerable.Repeat(Short, 20));
}

internal static class TextBenchmarkStatistics
{
    public static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0) return 0;
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) / 2 : sorted[middle];
    }

    public static double Percentile(double[] values, double percentile)
    {
        if (values.Length == 0) return 0;
        var sorted = values.Order().ToArray();
        return sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * percentile) - 1, 0, sorted.Length - 1)];
    }
}
