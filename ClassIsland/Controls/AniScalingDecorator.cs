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
        
        // Measure child with infinite width to get desired size
        Child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var desired = Child.DesiredSize;
        
        // Calculate required scale
        double scale = 1.0;
        if (availableSize.Width < desired.Width && availableSize.Width > 0 && IsAutoScalingEnabled && desired.Width > 0)
        {
            scale = availableSize.Width / desired.Width;
        }

        // Apply buffer
        var bufferSize = (Avalonia.Application.Current as App)?.Settings?.AutoScalingBufferFrameCount ?? 30;
        _scaleBuffer.Enqueue(scale);
        while (_scaleBuffer.Count > bufferSize)
        {
            _scaleBuffer.Dequeue();
        }
        scale = _scaleBuffer.Average();
        
        UpdateScale(scale);

        // We report that we occupy the available width (or desired width if smaller)
        return new Size(Math.Min(availableSize.Width, desired.Width), desired.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child == null) return base.ArrangeOverride(finalSize);
        
        // Ensure the scale transform is applied
        if (Child.RenderTransform != _scaleTransform)
        {
            Child.RenderTransform = _scaleTransform;
            Child.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
        }
        
        // Arrange the child at its desired size
        Child.Arrange(new Rect(0, 0, Child.DesiredSize.Width, finalSize.Height));
        return finalSize;
    }
    
    private void UpdateScale(double targetScale)
    {
        // Round to avoid jitter
        targetScale = Math.Round(targetScale, 4);

        if (Math.Abs(targetScale - _pendingScale) < 0.0001) return;

        var oldTarget = _pendingScale;
        _pendingScale = targetScale;
        
        // Update hysteresis timer interval based on settings
        _hysteresisTimer.Interval = TimeSpan.FromSeconds(
            (Avalonia.Application.Current as App)?.Settings?.AutoScalingBufferTimeWindow ?? 5.0
        );

        // Logic
        if (targetScale < _currentScale)
        {
            // Shrinking (Content growing) - Immediate
            _hysteresisTimer.Stop();
             AnimateTo(targetScale);
        }
        else
        {
            // Expanding (Content shrinking)
            // If the change is large (e.g. > 10% change towards 1.0, or huge jump), animate immediately
            if (targetScale - _currentScale > 0.1 || (oldTarget < 1.0 && targetScale >= 0.99)) 
            {
                 // If returning to full size or significant change, do it.
                 // User said "5s buffer ... unless change is large".
                 if (targetScale - _currentScale > 0.05) 
                 {
                     _hysteresisTimer.Stop();
                     AnimateTo(targetScale);
                 }
                 else
                 {
                     if (!_hysteresisTimer.IsEnabled) _hysteresisTimer.Start();
                 }
            }
            else
            {
                if (!_hysteresisTimer.IsEnabled) _hysteresisTimer.Start();
            }
        }
    }
    
    private void AnimateTo(double target)
    {
        _currentScale = target;
        // Simple linear interpolation animation or Avalonia animation
        // Since we are not in a visual tree suitable for standard Transitions sometimes,
        // manually animating properties is fine.
        // Or we can use Avalonia's animation system.
        
        // Cancel previous animation
        _animationCts?.Cancel();
        _animationCts = new CancellationTokenSource();
        var token = _animationCts.Token;

        var start = _scaleTransform.ScaleX;
        var end = target;
        
        // if diff is small, just set
        if (Math.Abs(start - end) < 0.001)
        {
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = end;
            return;
        }

        // Animation loop
        // 300ms duration
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
            
            // Cubic Ease Out
            var easing = 1 - Math.Pow(1 - progress, 3);
            var current = start + (end - start) * easing;
            
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = current;
            return true;
        }, TimeSpan.FromMilliseconds(16));
    }
}
