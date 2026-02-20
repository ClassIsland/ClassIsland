using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Services;
using ClassIsland.Models;

namespace ClassIsland.Controls;

public class AniScalingDecorator : Decorator
{
    private double _currentScale = 1.0;
    private double _pendingScale = 1.0;
    private DispatcherTimer _hysteresisTimer;
    private ScaleTransform _scaleTransform = new ScaleTransform();
    private CancellationTokenSource? _animationCts;
    private Queue<double> _scaleBuffer = new Queue<double>();
    
    public static readonly StyledProperty<bool> IsAutoScalingEnabledProperty =
        AvaloniaProperty.Register<AniScalingDecorator, bool>(nameof(IsAutoScalingEnabled), true);

    public bool IsAutoScalingEnabled
    {
        get => GetValue(IsAutoScalingEnabledProperty);
        set => SetValue(IsAutoScalingEnabledProperty, value);
    }
    
    private Settings? Settings => (Avalonia.Application.Current as App)?.Settings;

    public AniScalingDecorator()
    {
        _hysteresisTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _hysteresisTimer.Tick += (s, e) => 
        {
            _hysteresisTimer.Stop();
            AnimateTo(_pendingScale); 
        };
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child == null) return base.MeasureOverride(availableSize);
        
        // 测量子元素理想宽度
        Child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var desired = Child.DesiredSize;
        
        // 获取安全边距
        double safetyMargin = Settings?.AutoScalingSafetyMargin ?? 12.0; 
        double workableWidth = availableSize.Width - safetyMargin;

        // 计算所需缩放
        double instantScale = 1.0;
        
        if (workableWidth > 0 && desired.Width > 0 && IsAutoScalingEnabled && desired.Width > workableWidth)
        {
            instantScale = workableWidth / desired.Width;
        }

        // 应用缓冲
        var bufferSize = Settings?.AutoScalingBufferFrameCount ?? 30;
        _scaleBuffer.Enqueue(instantScale);
        while (_scaleBuffer.Count > bufferSize)
        {
            _scaleBuffer.Dequeue();
        }
        
        var averageScale = _scaleBuffer.Average();
        // 发生溢出时优先使用瞬时值（Min-Hold），确保立刻响应
        var targetScale = Math.Min(instantScale, averageScale);

        // 如果瞬时值小于当前目标值，强制立即缩小
        if (instantScale < targetScale) targetScale = instantScale;
        
        UpdateScale(targetScale);

        // 报告占用宽度（可用宽度或理想宽度）
        return new Size(Math.Min(availableSize.Width, desired.Width), desired.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child == null) return base.ArrangeOverride(finalSize);
        
        // 确保应用 ScaleTransform
        if (Child.RenderTransform != _scaleTransform)
        {
            Child.RenderTransform = _scaleTransform;
        }

        // 确定对齐方式：0=左, 1=中, 2=右
        var dockingLocation = Settings?.WindowDockingLocation ?? 1;
        int alignment = dockingLocation % 3; 
        
        // 垂直原点：0-2 顶部(0), 3-5 底部(1)
        double originY = dockingLocation >= 3 ? 1.0 : 0.0;
        
        double safetyMarginTotal = Settings?.AutoScalingSafetyMargin ?? 12.0;
        double safetyMarginSide = safetyMarginTotal / 2.0;
        
        double x = 0.0;
        double originX = 0.0;

        // 根据对齐方式计算排列位置
        switch (alignment)
        {
            case 0: // 左对齐
                originX = 0.0;
                x = safetyMarginSide;
                break;
            case 1: // 居中
                originX = 0.5;
                x = (finalSize.Width - Child.DesiredSize.Width) / 2.0;
                break;
            case 2: // 右对齐
                originX = 1.0;
                // 确保缩放后的视觉右边缘位于 finalSize.Width - Margin
                x = finalSize.Width - Child.DesiredSize.Width - safetyMarginSide;
                break;
        }
        
        Child.RenderTransformOrigin = new RelativePoint(originX, originY, RelativeUnit.Relative);
        
        // 排列子元素
        Child.Arrange(new Rect(new Point(x, 0), Child.DesiredSize));
        
        return finalSize;
    }
    
    private void UpdateScale(double targetScale)
    {
        // 避免微小抖动
        targetScale = Math.Round(targetScale, 4);

        if (Math.Abs(targetScale - _pendingScale) < 0.0001) return;

        var oldTarget = _pendingScale;
        _pendingScale = targetScale;
        
        _hysteresisTimer.Interval = TimeSpan.FromSeconds(
            Settings?.AutoScalingBufferTimeWindow ?? 5.0
        );

        // 逻辑处理
        if (targetScale < _currentScale)
        {
            // 缩小（内容变宽）：立即执行
            _hysteresisTimer.Stop();
             AnimateTo(targetScale);
        }
        else
        {
            // 放大（内容变窄或恢复）：
            // 如果变化显著 (>0.05) 或即将恢复到原状 (>=0.99)，立即执行
            bool isSignificantChange = (targetScale - _currentScale > 0.05);
            bool isRestoringToFull = (oldTarget < 1.0 && targetScale >= 0.99);
            
            if (isSignificantChange || isRestoringToFull) 
            {
                 _hysteresisTimer.Stop();
                 AnimateTo(targetScale);
            }
            else
            {
                // 微小变化：进入缓冲延迟
                if (!_hysteresisTimer.IsEnabled) _hysteresisTimer.Start();
            }
        }
    }
    
    private void AnimateTo(double target)
    {
        _currentScale = target;
        
        _animationCts?.Cancel();
        _animationCts = new CancellationTokenSource();
        var token = _animationCts.Token;

        var start = _scaleTransform.ScaleX;
        var end = target;
        
        if (Math.Abs(start - end) < 0.001)
        {
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = end;
            return;
        }

        // 动画循环：300ms cubic-ease-out
        var duration = TimeSpan.FromMilliseconds(300);
        var startTime = DateTime.Now;
        
        DispatcherTimer.Run(() =>
        {
            if (token.IsCancellationRequested) return false;
            
            var now = DateTime.Now;
            var elapsed = now - startTime;
            var progress = elapsed.TotalMilliseconds / duration.TotalMilliseconds;
            
            if (progress >= 1.0)
            {
                _scaleTransform.ScaleX = _scaleTransform.ScaleY = end;
                return false;
            }
            
            var easing = 1 - Math.Pow(1 - progress, 3);
            var current = start + (end - start) * easing;
            
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = current;
            return true;
        }, TimeSpan.FromMilliseconds(16));
    }
}
