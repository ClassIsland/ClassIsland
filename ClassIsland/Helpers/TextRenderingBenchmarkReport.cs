using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsland.Models;

namespace ClassIsland.Helpers;

internal static class TextRenderingBenchmarkReport
{
    public const string MeasurementNotes = """
        首帧耗时从创建控件开始，到包含新内容的合成批次 Rendered 完成为止；这不是显示器呈现延迟。
        所有重复项均实际创建并参与布局，首帧只绘制可见区域。Markdown 解析源码，TextBlock 原样显示同一源码，排版并不相同。
        每次渲染采样先清空并等待清空帧，再在计时外执行完整 GC；结束采样不强制 GC。差值保留正负号。
        内存、累计托管分配与 GC 次数为进程级观测，包含应用其他活动及采样开销，不代表控件独占内存或存活对象大小。
        自动滚动以 600 DIP/s 运行 10 秒，到边界反向。UI 动画回调频率不是 GPU FPS 或显示器实际呈现帧率。
        滚动间隔使用单调时钟，相邻回调统计不包含启动到首次回调的间隔；百分位采用 nearest-rank，超过阈值的比例不是丢帧率。
        覆盖率为滚动到的最远位置 / 可滚动范围；滚动距离按时间轨迹计算，卡顿跨越的路径不保证逐像素绘制。
        进度和结果表仅在采样区间外更新；报告生成和导出不计入测量。预热不进入正式统计，预热不等同于清空字体或渲染缓存。
        """;

    public static async Task ExportAsync(Stream stream, TextBenchmarkSession session)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true);
        await WriteEntryAsync(archive, "report.md", CreateMarkdown(session));
        var json = JsonSerializer.Serialize(session, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        await WriteEntryAsync(archive, "results.json", json);
    }

    private static async Task WriteEntryAsync(ZipArchive archive, string name, string content)
    {
        await using var stream = archive.CreateEntry(name).Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content);
    }

    public static string CreateMarkdown(TextBenchmarkSession session)
    {
        var report = new StringBuilder("# 文本渲染与滚动性能报告\n\n");
        report.AppendLine($"开始：{session.StartedAt:O}；结束：{session.FinishedAt:O}；状态：{session.Status}\n");
        report.AppendLine($"计划 {session.PlannedCases.Count} 组；已记录 {session.Results.Count} 组；正式渲染 {session.Results.Sum(x => x.Samples.Count)} 次。\n");
        report.AppendLine($"每组预热 {session.WarmupCount} 次，正式采样 {session.SamplesPerCase} 次；滚动 {session.ScrollSeconds} 秒，{session.ScrollSpeedDipPerSecond} DIP/s。\n");
        if (session.InterruptionReason != null)
            report.AppendLine($"中断原因：{Cell(session.InterruptionReason)}\n");
        report.AppendLine("## 环境\n\n| 项目 | 值 |\n| --- | --- |");
        foreach (var (key, value) in session.Environment)
            report.AppendLine($"| {Cell(key)} | {Cell(value)} |");
        report.AppendLine("\n## 测量口径\n");
        report.AppendLine(MeasurementNotes);
        report.AppendLine("\n## 渲染结果\n\n耗时为中位数（最小–最大），内存为中位数。— 表示缺失，失败不计为零。\n");
        report.AppendLine("| 组合 | 状态 | 次数 | 创建与挂载 ms | 首帧 ms | 堆增量 MiB | 工作集增量 MiB | 托管分配 MiB | Gen0/1/2 增量 |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var result in session.Results)
        {
            var samples = result.Samples;
            var gc = samples.Count == 0 ? "—" : string.Join(" / ", new[]
            {
                TextBenchmarkStatistics.Median(samples.Select(x => (double)x.Memory.Gen0)),
                TextBenchmarkStatistics.Median(samples.Select(x => (double)x.Memory.Gen1)),
                TextBenchmarkStatistics.Median(samples.Select(x => (double)x.Memory.Gen2))
            }.Select(x => F(x)));
            report.AppendLine($"| {result.Name} | {Cell(result.Status)} | {samples.Count} | {Range(samples.Select(x => x.CreationMs).ToArray())} | {Range(samples.Select(x => x.FirstFrameMs).ToArray())} | {Memory(samples.Select(x => (double)x.Memory.HeapBytes).ToArray())} | {Memory(samples.Where(x => x.Memory.WorkingSetBytes.HasValue).Select(x => (double)x.Memory.WorkingSetBytes!.Value).ToArray())} | {Memory(samples.Select(x => (double)x.Memory.AllocatedBytes).ToArray())} | {gc} |");
        }
        report.AppendLine("\n## 自动滚动\n\n| 组合 | 状态 | 实际秒数 | 回调次数 | UI 回调 Hz | P50 / P95 / P99 / 最大 ms | >16.67 / >33.33 / >50 ms 比例 | 距离 DIP | 最远位置 / 范围 DIP | 覆盖率 |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var result in session.Results)
        {
            var scroll = result.Scroll;
            if (scroll == null || scroll.Status != "完成")
            {
                report.AppendLine($"| {result.Name} | {Cell(scroll?.Status ?? "未测试")} | — | — | — | — | — | — | — | — |");
                continue;
            }
            report.AppendLine($"| {result.Name} | 完成 | {F(scroll.DurationSeconds)} | {scroll.CallbackCount} | {F(scroll.CallbackHz)} | {F(scroll.P50Ms)} / {F(scroll.P95Ms)} / {F(scroll.P99Ms)} / {F(scroll.MaxMs)} | {F(scroll.Over16MsRatio * 100)}% / {F(scroll.Over33MsRatio * 100)}% / {F(scroll.Over50MsRatio * 100)}% | {F(scroll.DistanceDip)} | {F(scroll.MaxOffsetDip)} / {F(scroll.ScrollRangeDip)} | {F(scroll.CoverageRatio * 100)}% |");
        }
        report.AppendLine("\n### 滚动内存增量\n\n| 组合 | 堆 MiB | 工作集 MiB | 托管分配 MiB | Gen0/1/2 |\n| --- | --- | --- | --- | --- |");
        foreach (var result in session.Results.Where(x => x.Scroll?.Status == "完成"))
        {
            var memory = result.Scroll!.Memory;
            report.AppendLine($"| {result.Name} | {F(memory.HeapBytes / 1048576.0)} | {(memory.WorkingSetBytes is { } bytes ? F(bytes / 1048576.0) : "—")} | {F(memory.AllocatedBytes / 1048576.0)} | {memory.Gen0}/{memory.Gen1}/{memory.Gen2} |");
        }
        report.AppendLine("\n## 同条件实现对比\n\n比值为 Markdown 首帧中位数 / TextBlock 首帧中位数；不作通过或失败判定。\n\n| 文本 | 数量 | 比值 |\n| --- | --- | --- |");
        foreach (var group in session.Results.GroupBy(x => (x.Case.IsLong, x.Case.Count)))
        {
            var plain = group.FirstOrDefault(x => !x.Case.IsMarkdown && x.Status == "完成" && x.Samples.Count > 0);
            var markdown = group.FirstOrDefault(x => x.Case.IsMarkdown && x.Status == "完成" && x.Samples.Count > 0);
            if (plain is { MedianFirstFrameMs: > 0 } && markdown != null)
                report.AppendLine($"| {(group.Key.IsLong ? "长文本" : "短文本")} | {group.Key.Count} | {F(markdown.MedianFirstFrameMs!.Value / plain.MedianFirstFrameMs.Value)} |");
        }
        report.AppendLine("\n## 异常与未完成项\n");
        foreach (var result in session.Results.Where(x => x.Error != null))
            report.AppendLine($"- {result.Name}：{Cell(result.Error!)}");
        foreach (var testCase in session.PlannedCases.Where(x => session.Results.All(r => r.Case != x || r.Status != "完成")))
            report.AppendLine($"- 未完成：{testCase.Name}");
        report.AppendLine("\n## 固定输入\n");
        report.AppendLine($"短文本 {session.ShortSource.Length} UTF-16 代码单元；长文本 {session.LongSource.Length}，为短文本重复 20 次。完整源码和逐次原始数据见 results.json。\n");
        report.AppendLine("```markdown\n" + session.ShortSource + "```");
        return report.ToString();
    }

    private static string Range(double[] values) => values.Length == 0 ? "—" :
        $"{F(TextBenchmarkStatistics.Median(values))} ({F(values.Min())}–{F(values.Max())})";

    private static string Memory(double[] values) => values.Length == 0 ? "—" :
        F(TextBenchmarkStatistics.Median(values) / 1048576);

    private static string F(double value) => FormattableString.Invariant($"{value:F2}");
    private static string Cell(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
}
