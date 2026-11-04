using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.ViewModels;

namespace ClassIsland.Views;

public partial class TextRenderingBenchmarkView : ViewBase
{
    private readonly TextRenderingBenchmarkViewModel _viewModel = new();
    private CancellationTokenSource? _runCancellation;
    private TextBenchmarkSession? _session;
    private TextBenchmarkResult? _renderedResult;
    private TopLevel? _observedRoot;
    private Size _runBounds;
    private Size _runViewport;
    private double _runScaling;
    private string? _interruptionReason;
    private bool _selectionUpdate;
    private bool _selectionQueued;

    public TextRenderingBenchmarkView()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += SelectionChanged;
        NavigatedFrom += (_, _) => CancelRun("已离开测试视图");
        Closed += (_, _) => CancelRun("测试视图已关闭");
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _observedRoot = TopLevel;
        if (_observedRoot != null)
            _observedRoot.PropertyChanged += RootPropertyChanged;
        if (_session == null)
            QueueSelectionRun();
        else
            _viewModel.CanScroll = !_viewModel.IsBusy && _renderedResult != null &&
                                   _renderedResult.Case == _viewModel.SelectedCase;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        CancelRun("测试视图已卸载");
        if (_observedRoot != null)
            _observedRoot.PropertyChanged -= RootPropertyChanged;
        _observedRoot = null;
        base.OnUnloaded(e);
    }

    private void RootPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(TopLevel.RenderScaling) ||
            e.Property == Window.WindowStateProperty && _observedRoot is Window { WindowState: WindowState.Minimized })
            CancelRun("窗口已最小化或显示缩放发生变化");
    }

    private void SelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TextRenderingBenchmarkViewModel.LengthIndex)
            or nameof(TextRenderingBenchmarkViewModel.SelectedCount)
            or nameof(TextRenderingBenchmarkViewModel.RendererIndex))
            QueueSelectionRun();
    }

    private void QueueSelectionRun()
    {
        if (_selectionUpdate || _viewModel.IsBusy || !IsLoaded || _selectionQueued) return;
        _selectionQueued = true;
        Dispatcher.UIThread.Post(async () =>
        {
            _selectionQueued = false;
            if (IsLoaded && !_viewModel.IsBusy)
                await RunAsync(false);
        }, DispatcherPriority.Background);
    }

    private async void RunCurrent_OnClick(object? sender, RoutedEventArgs e) => await RunAsync(false);
    private async void RunMatrix_OnClick(object? sender, RoutedEventArgs e) => await RunAsync(true);
    private async void Scroll_OnClick(object? sender, RoutedEventArgs e) => await RunAsync(false, true);
    private void Cancel_OnClick(object? sender, RoutedEventArgs e) => CancelRun("用户取消");

    private void CancelRun(string reason)
    {
        if (_runCancellation == null) return;
        _interruptionReason ??= reason;
        _runCancellation.Cancel();
    }

    private async Task RunAsync(bool matrix, bool scrollOnly = false)
    {
        if (_viewModel.IsBusy || !IsLoaded || TopLevel == null) return;
        if (scrollOnly && (_session == null || _renderedResult == null ||
                           _renderedResult.Case != _viewModel.SelectedCase)) return;

        _viewModel.IsBusy = true;
        _viewModel.CanScroll = false;
        _viewModel.CanExport = false;
        _interruptionReason = null;
        using var cancellation = new CancellationTokenSource();
        _runCancellation = cancellation;
        var token = cancellation.Token;
        using var process = Process.GetCurrentProcess();
        try
        {
            // Settle the disabled toolbar before capturing the invariant viewport and memory baseline.
            await WaitForRenderedFrameAsync(token);
            _runBounds = BenchmarkScroller.Bounds.Size;
            _runViewport = BenchmarkScroller.Viewport;
            _runScaling = TopLevel.RenderScaling;
            EnsureEnvironment(token);
            if (!scrollOnly)
            {
                var cases = matrix
                    ? (from isLong in new[] { false, true }
                       from count in _viewModel.CountOptions
                       from markdown in new[] { false, true }
                       select new TextBenchmarkCase(isLong, count, markdown)).ToList()
                    : new List<TextBenchmarkCase> { _viewModel.SelectedCase };
                _session = new TextBenchmarkSession
                {
                    IsMatrix = matrix, PlannedCases = cases, Environment = CaptureEnvironment()
                };
                _viewModel.Results.Clear();
                _renderedResult = null;
                for (var index = 0; index < cases.Count; index++)
                {
                    EnsureEnvironment(token);
                    var testCase = cases[index];
                    _selectionUpdate = true;
                    _viewModel.SelectCase(testCase);
                    _selectionUpdate = false;
                    _viewModel.Progress = $"组合 {index + 1}/{cases.Count}：{testCase.Name}";
                    var result = new TextBenchmarkResult(testCase);
                    _session.Results.Add(result);
                    try
                    {
                        if (matrix)
                        {
                            _viewModel.Phase = "预热 1/1";
                            await MeasureRenderAsync(testCase, 0, process, token);
                        }
                        for (var iteration = 1; iteration <= _session.SamplesPerCase; iteration++)
                        {
                            _viewModel.Phase = $"渲染采样 {iteration}/{_session.SamplesPerCase}";
                            result.Samples.Add(await MeasureRenderAsync(testCase, iteration, process, token));
                        }
                        _renderedResult = result;
                        if (matrix)
                        {
                            _viewModel.Phase = "自动滚动 10 秒";
                            result.Scroll = await MeasureScrollAsync(process, token);
                        }
                        result.Status = "完成";
                    }
                    catch (OperationCanceledException)
                    {
                        result.Status = "已中断";
                        result.Error = _interruptionReason ?? "用户取消";
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.Status = "失败";
                        result.Error = ex.ToString();
                        _renderedResult = null;
                    }
                    _viewModel.RefreshResults(_session);
                    _viewModel.CanExport = false;
                    ShowLatest(result);
                }
            }
            else
            {
                _session!.Status = "进行中";
                _viewModel.Phase = "自动滚动 10 秒";
                _renderedResult!.Scroll = null;
                _renderedResult.Scroll = await MeasureScrollAsync(process, token);
                ShowLatest(_renderedResult);
            }
            _session!.Status = _session.Results.Count != _session.PlannedCases.Count ||
                               _session.Results.Any(x => x.Status != "完成") ? "部分完成" : "完成";
            _viewModel.Phase = _session.Status;
        }
        catch (OperationCanceledException)
        {
            if (_session != null)
            {
                _session.Status = "已中断";
                _session.InterruptionReason = _interruptionReason ?? "用户取消";
            }
            _viewModel.Phase = _interruptionReason ?? "已取消";
            if (_session?.Results.LastOrDefault() is { } last)
                ShowLatest(last);
        }
        catch (Exception ex)
        {
            if (_session != null)
            {
                _session.Status = "失败";
                _session.InterruptionReason = ex.ToString();
            }
            _viewModel.Phase = $"测试失败：{ex.Message}";
        }
        finally
        {
            _runCancellation = null;
            _selectionUpdate = false;
            _viewModel.IsBusy = false;
            if (_session != null)
            {
                _session.FinishedAt = DateTimeOffset.Now;
                _viewModel.RefreshResults(_session);
            }
            _viewModel.CanScroll = IsLoaded && _renderedResult != null &&
                                   _renderedResult.Case == _viewModel.SelectedCase;
        }
    }

    private async Task<TextBenchmarkRenderSample> MeasureRenderAsync(TextBenchmarkCase testCase, int iteration,
        Process process, CancellationToken token)
    {
        _renderedResult = null;
        var source = testCase.IsLong ? TextBenchmarkCorpus.Long : TextBenchmarkCorpus.Short;
        BenchmarkPanel.Children.Clear();
        BenchmarkScroller.Offset = default;
        await WaitForRenderedFrameAsync(token);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        EnsureEnvironment(token);
        var baseline = TextBenchmarkMemory.Capture(process);
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < testCase.Count; index++)
        {
            token.ThrowIfCancellationRequested();
            Control control = testCase.IsMarkdown ? MarkdownConvertHelper.Engine.Transform(source) : new TextBlock
            {
                Text = source, TextWrapping = TextWrapping.Wrap, FontSize = 16
            };
            BenchmarkPanel.Children.Add(control);
        }
        var creationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        await WaitForRenderedFrameAsync(token);
        var firstFrameMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var memory = TextBenchmarkMemory.Capture(process).Delta(baseline);
        EnsureEnvironment(token);
        return new TextBenchmarkRenderSample(iteration, creationMs, firstFrameMs, memory);
    }

    private async Task<TextBenchmarkScrollResult> MeasureScrollAsync(Process process, CancellationToken token)
    {
        EnsureEnvironment(token);
        BenchmarkScroller.Offset = default;
        await WaitForRenderedFrameAsync(token);
        var range = Math.Max(0, BenchmarkScroller.Extent.Height - BenchmarkScroller.Viewport.Height);
        if (range <= 0) return new TextBenchmarkScrollResult();

        // Reserve well above ordinary refresh rates; no resizing or result binding updates during measurement.
        var intervals = new double[32768];
        var completion = new TaskCompletionSource<TextBenchmarkScrollResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = token.Register(() => completion.TrySetCanceled(token));
        var root = TopLevel!;
        var baseline = TextBenchmarkMemory.Capture(process);
        var started = Stopwatch.GetTimestamp();
        var previous = 0L;
        var count = 0;
        var intervalCount = 0;
        var maxOffset = 0.0;
        root.RequestAnimationFrame(Frame);
        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(40), token);
        }
        finally
        {
            // Animation callbacks cannot be unregistered; a timed-out run must stop rescheduling them.
            completion.TrySetCanceled();
        }

        void Frame(TimeSpan timestamp)
        {
            if (completion.Task.IsCompleted) return;
            try
            {
                EnsureEnvironment(token);
                var now = Stopwatch.GetTimestamp();
                var elapsed = Stopwatch.GetElapsedTime(started, now).TotalSeconds;
                if (previous != 0)
                {
                    if (intervalCount == intervals.Length)
                        throw new InvalidOperationException("滚动回调超出预分配容量，当前组不纳入统计。");
                    intervals[intervalCount++] = Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds;
                }
                previous = now;
                count++;
                var phase = Math.Min(elapsed, 10) * 600 % (2 * range);
                var offset = phase <= range ? phase : 2 * range - phase;
                BenchmarkScroller.Offset = new Vector(0, offset);
                maxOffset = Math.Max(maxOffset, offset);
                if (elapsed < 10)
                {
                    root.RequestAnimationFrame(Frame);
                    return;
                }
                var memory = TextBenchmarkMemory.Capture(process).Delta(baseline);
                completion.TrySetResult(new TextBenchmarkScrollResult
                {
                    Status = "完成", DurationSeconds = elapsed, CallbackCount = count,
                    IntervalsMs = intervals[..intervalCount], Memory = memory,
                    DistanceDip = 6000, MaxOffsetDip = maxOffset, ScrollRangeDip = range
                });
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }
    }

    private async Task WaitForRenderedFrameAsync(CancellationToken token)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var compositor = ElementComposition.GetElementVisual(BenchmarkPanel)?.Compositor
                         ?? throw new InvalidOperationException("测试内容尚未连接到可视树。");
        var active = true;
        try
        {
            compositor.RequestCompositionUpdate(() =>
            {
                if (!active || token.IsCancellationRequested) return;
                try
                {
                    _ = CompleteAsync(compositor.RequestCompositionBatchCommitAsync().Rendered);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            });
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), token);
        }
        finally
        {
            active = false;
        }

        async Task CompleteAsync(Task rendered)
        {
            try
            {
                await rendered.ConfigureAwait(false);
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }
    }

    private void EnsureEnvironment(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsLoaded || !IsEffectivelyVisible || TopLevel == null ||
            TopLevel is Window { WindowState: WindowState.Minimized } ||
            BenchmarkScroller.Bounds.Size != _runBounds || BenchmarkScroller.Viewport != _runViewport ||
            TopLevel.RenderScaling != _runScaling || _runBounds != new Size(720, 360))
        {
            CancelRun("测试视图已隐藏、视口或显示缩放发生变化");
            token.ThrowIfCancellationRequested();
        }
    }

    private Dictionary<string, string> CaptureEnvironment() => new()
    {
        ["应用版本"] = AppBase.AppVersion.ToString(),
        [".NET"] = RuntimeInformation.FrameworkDescription,
        ["Avalonia"] = typeof(Control).Assembly.GetName().Version?.ToString() ?? "未知",
        ["Markdown"] = typeof(Markdown.Avalonia.Markdown).Assembly.GetName().Version?.ToString() ?? "未知",
        ["操作系统"] = RuntimeInformation.OSDescription,
        ["系统架构"] = RuntimeInformation.OSArchitecture.ToString(),
        ["进程架构"] = RuntimeInformation.ProcessArchitecture.ToString(),
#if DEBUG
        ["构建配置"] = "Debug",
#else
        ["构建配置"] = "Release",
#endif
        ["调试器"] = Debugger.IsAttached.ToString(),
        ["字体"] = FontFamily.ToString(),
        ["基础字号"] = "16 DIP",
        ["主题"] = ActualThemeVariant.ToString(),
        ["显示缩放"] = _runScaling.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["ScrollViewer 尺寸 DIP"] = _runBounds.ToString(),
        ["实际视口 DIP"] = _runViewport.ToString()
    };

    private void ShowLatest(TextBenchmarkResult result)
    {
        var sample = result.Samples.LastOrDefault();
        _viewModel.LatestMetrics = $"{result.Name}：首帧 {result.RenderSummary}；创建与挂载 {result.CreationSummary}；{result.MemorySummary}\n" +
            (sample == null ? result.Status :
                $"最近一次工作集增量 {(sample.Memory.WorkingSetBytes is { } bytes ? $"{bytes / 1048576.0:+0.00;-0.00;0.00} MiB" : "不支持")}；GC {sample.Memory.Gen0}/{sample.Memory.Gen1}/{sample.Memory.Gen2}；滚动：{result.ScrollSummary}");
    }

    private async void Export_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsBusy || _session == null || TopLevel == null) return;
        _viewModel.IsBusy = true;
        _viewModel.CanExport = false;
        _viewModel.CanScroll = false;
        try
        {
            var root = TopLevel;
            var path = await PlatformServices.FilePickerService.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "导出文本性能报告（Markdown 与 JSON）",
                SuggestedFileName = $"TextBenchmark_{_session.StartedAt:yyyyMMdd_HHmmss}.zip",
                FileTypeChoices = [new FilePickerFileType("性能报告 ZIP") { Patterns = ["*.zip"] }],
                DefaultExtension = "zip"
            }, root);
            if (path == null) return;
            using var file = await PlatformServices.FilePickerService.GetFileAsync(path, root)
                             ?? throw new FileNotFoundException("无法打开报告保存位置。", path);
            await using var stream = await file.OpenWriteAsync();
            if (stream.CanSeek)
            {
                stream.SetLength(0);
                stream.Position = 0;
            }
            await TextRenderingBenchmarkReport.ExportAsync(stream, _session);
            this.ShowSuccessToast("报告已导出，包含 report.md 与 results.json。");
        }
        catch (Exception ex)
        {
            this.ShowErrorToast("无法导出性能报告", ex);
        }
        finally
        {
            _viewModel.IsBusy = false;
            _viewModel.CanExport = _session.Results.Count > 0;
            _viewModel.CanScroll = IsLoaded && _renderedResult != null &&
                                   _renderedResult.Case == _viewModel.SelectedCase;
        }
    }
}
