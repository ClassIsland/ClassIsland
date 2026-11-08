using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace ClassIsland.Android.Controls;

internal sealed class AndroidSkiaGpuMetrics : Control
{
    private readonly TaskCompletionSource<string> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private AndroidSkiaGpuMetrics()
    {
        Width = 1;
        Height = 1;
        IsHitTestVisible = false;
    }

    internal static async Task<string> ReadAsync(TopLevel? root)
    {
        var layer = root == null ? null : OverlayLayer.GetOverlayLayer(root);
        if (layer == null)
        {
            return "暂不可用";
        }

        var probe = new AndroidSkiaGpuMetrics();
        layer.Children.Add(probe);
        try
        {
            // A render pass provides safe access to the active GPU context without retaining it.
            return await probe._result.Task.WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (TimeoutException)
        {
            return "暂不可用";
        }
        finally
        {
            layer.Children.Remove(probe);
        }
    }

    public override void Render(DrawingContext context)
    {
        context.Custom(new SampleOperation(new Rect(Bounds.Size), _result));
    }

    private sealed class SampleOperation(Rect bounds, TaskCompletionSource<string> result) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;

        public bool HitTest(Point point) => false;

        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

        public void Render(ImmediateDrawingContext context)
        {
            if (result.Task.IsCompleted)
            {
                return;
            }

            var reading = "暂不可用";
            try
            {
                var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (feature != null)
                {
                    using var lease = feature.Lease();
                    if (lease.GrContext is { IsAbandoned: false } gpu)
                    {
                        gpu.GetResourceCacheUsage(out var count, out var bytes);
                        var limit = gpu.GetResourceCacheLimit();
                        reading = $"{bytes / 1048576.0:F1} / {limit / 1048576.0:F1} MiB（{count} 个资源）";
                    }
                }
            }
            catch (Exception exception)
            {
                global::Android.Util.Log.Warn(nameof(AndroidSkiaGpuMetrics), exception.ToString());
            }
            result.TrySetResult(reading);
        }

        public void Dispose()
        {
        }
    }
}
