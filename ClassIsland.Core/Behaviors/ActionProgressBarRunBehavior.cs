using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using ClassIsland.Shared.Models.Automation;
namespace ClassIsland.Core.Behaviors;

/// <summary>
///     ProgressBarRunBehavior：
///     监听 DataContext 的 IsWorking / Progress 属性变化，用于实现：开始为 indeterminate、更新为 determinate、结束时进度到 100 再渐隐并隐藏。
/// </summary>
public class ProgressBarRunBehavior : Behavior<ProgressBar>
{
    static readonly TimeSpan _fadeDuration = TimeSpan.FromMilliseconds(300);
    static readonly TimeSpan _valueAnimWait = TimeSpan.FromMilliseconds(220);
    ActionItem? _actionItem;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is null) return;
        AssociatedObject.DataContextChanged += OnDataContextChanged;
        AttachToDataContext(AssociatedObject.DataContext as ActionItem);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is null) return;
        AssociatedObject.DataContextChanged -= OnDataContextChanged;
        DetachFromCurrent();
    }

    void OnDataContextChanged(object? sender, EventArgs e)
    {
        AttachToDataContext(AssociatedObject?.DataContext as ActionItem);
    }

    void AttachToDataContext(ActionItem? actionItem)
    {
        if (actionItem is null) return;

        DetachFromCurrent();

        _actionItem = actionItem;
        actionItem.PropertyChanged += OnVmPropertyChanged;

        // 初始同步一次状态（比如页面打开时）
        _ = Dispatcher.UIThread.InvokeAsync(SyncState);
    }

    void DetachFromCurrent()
    {
        if (_actionItem != null)
        {
            _actionItem.PropertyChanged -= OnVmPropertyChanged;
            _actionItem = null;
        }
    }

    void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 只关注具体两个属性（避免多余处理）；
        if (e.PropertyName == nameof(ActionItem.IsWorking))
        {
            if (!_actionItem.IsWorking)
                // 停止：执行结束序列（异步但不阻塞 UI 线程）；
                _ = OnStopSequenceAsync();
            else
                // 开始：确保可见与不透明；
                _ = Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (AssociatedObject is null) return;
                    AssociatedObject.IsVisible = true;
                    AssociatedObject.Opacity = 1; // 恢复不透明度；
                    AssociatedObject.IsEnabled = true;
                });
        }
        else if (e.PropertyName == nameof(ActionItem.Progress) && _actionItem.IsWorking)
        {
            // 进度变更：若为数字则立即显示 determinate 并设置 Value；
            var p = _actionItem.Progress;
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (AssociatedObject is null) return;
                AssociatedObject.IsVisible = true;
                AssociatedObject.Opacity = 1; // 保证可见；
                if (p != null)
                {
                    // 直接设 Value，会触发 XAML 中对 Value 的 DoubleTransition，从而视觉上平滑过渡；
                    AssociatedObject.Value = p.Value;
                }
                // 若 progress 为 null，indeterminate 由 XAML 绑定决定；这里不强行改 Value；
            });
        }
    }

    /// <summary>
    ///     页面打开或 DataContext 切换时，做一次同步：根据当前 IsWorking / Progress 决定初始表现。
    /// </summary>
    void SyncState()
    {
        if (AssociatedObject is null || _actionItem is null) return;
        var progress = _actionItem.Progress;

        AssociatedObject.IsVisible = _actionItem.IsWorking;
        AssociatedObject.Opacity = 1;
        AssociatedObject.Value = progress ?? 0;
    }

    /// <summary>
    ///     停止时的序列：把 Value 设为 100（触发宽度动画）、等待动画、开始渐隐、等待渐隐结束、最后隐藏控制并禁用。
    /// </summary>
    async Task OnStopSequenceAsync()
    {
        if (AssociatedObject is null) return;

        // 1) 立即确保可见并把 Value 设为 100，触发 Value 的过渡动画；
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (AssociatedObject is null) return;
            AssociatedObject.IsVisible = true;
            AssociatedObject.Value = 100; // 强制到 100；
            AssociatedObject.Opacity = 1; // 确保从不透明开始；
        });

        // 2) 等待 Value 动画完成；
        await Task.Delay(_valueAnimWait).ConfigureAwait(false);

        // 3) 开始渐隐（设置 Opacity 为 0，XAML 的 Opacity transition 会生效）；
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (AssociatedObject is null) return;
            AssociatedObject.Opacity = 0;
        });

        // 4) 等待渐隐完成，然后隐藏并禁用控件；
        await Task.Delay(_fadeDuration).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (AssociatedObject is null) return;
            AssociatedObject.IsVisible = false;
            AssociatedObject.Opacity = 1;
            AssociatedObject.Value = 0;
        });
    }
}