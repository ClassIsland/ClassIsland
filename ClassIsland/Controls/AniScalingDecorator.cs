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
        
        // Measure child with infinite width to get desired size
        Child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var desired = Child.DesiredSize;
        
        // Safety margin on both sides (e.g. 6.0 on left, 6.0 on right = 12.0 total)
        // User requested "keep a little safety area on left and right".
        double safetyMargin = 12.0; 
        double workableWidth = availableSize.Width - safetyMargin;

        // Calculate required scale
        // We use Settings.Scale if available, otherwise 1.0 as base? 
        // Actually, LayoutTransform should handle Settings.Scale.
        // We calculate scale relative to the space we have (which is already transformed if inside LayoutTransform).
        double instantScale = 1.0;
        
        if (workableWidth > 0 && desired.Width > 0 && IsAutoScalingEnabled && desired.Width > workableWidth)
        {
            instantScale = workableWidth / desired.Width;
        }

        // Apply buffer
        var bufferSize = Settings?.AutoScalingBufferFrameCount ?? 30;
        _scaleBuffer.Enqueue(instantScale);
        while (_scaleBuffer.Count > bufferSize)
        {
            _scaleBuffer.Dequeue();
        }
        
        var averageScale = _scaleBuffer.Average();
        // If instant scale is significantly smaller (overflow), we strictly prefer it (Min hold).
        var targetScale = Math.Min(instantScale, averageScale);

        // Force immediate shrink if instant scale is smaller than current target
        if (instantScale < targetScale) targetScale = instantScale;
        
        UpdateScale(targetScale);

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
        }

        // Determine alignment
        // 0, 3: Left; 1, 4: Center; 2, 5: Right
        var dockingLocation = Settings?.WindowDockingLocation ?? 1;
        int alignment = dockingLocation % 3; // 0=Left, 1=Center, 2=Right
        
        // Y Origin: 0-2 Top (0), 3-5 Bottom (1)
        double originY = dockingLocation >= 3 ? 1.0 : 0.0;
        
        double safetyMarginTotal = 12.0;
        double safetyMarginSide = safetyMarginTotal / 2.0;
        
        double x = 0.0;
        double originX = 0.0;

        // Logic:
        // finalSize is likely the constrained size (availableSize from Measure).
        // scaleTransform scales the Child VISUALLY around RenderTransformOrigin.
        // layout slot of Child remains Child.DesiredSize (or whatever we pass to Arrange).
        // but we should pass Child.DesiredSize to Arrange so it renders correctly internally.
        
        // If we want to align the SCALED content.
        
        switch (alignment)
        {
            case 0: // Left
                originX = 0.0;
                // Align left with safety margin
                x = safetyMarginSide;
                break;
            case 1: // Center
                originX = 0.5;
                // Align center in finalSize
                x = (finalSize.Width - Child.DesiredSize.Width) / 2.0;
                break;
            case 2: // Right
                originX = 1.0;
                // Align right with safety margin
                // We want Visual Right Edge to be at finalSize.Width - safetyMarginSide.
                // Child.DesiredSize.Width is the layout Width.
                // Pivot at 1.0 (Right Edge of Child).
                // Arrange at X such that Child.Right maps to Visual Right.
                // Child.Right = x + Child.DesiredSize.Width.
                // Visual Right (at Scale 1.0) = Child.Right.
                // Visual Right (at Scale < 1.0, Pivot Right) = Child.Right.
                // So if we align the Layout Right Edge to (FinalWidth - Margin), the Visual Right Edge will be there too.
                x = finalSize.Width - Child.DesiredSize.Width - safetyMarginSide;
                break;
        }
        
        Child.RenderTransformOrigin = new RelativePoint(originX, originY, RelativeUnit.Relative);
        
        // Arrange the child
        Child.Arrange(new Rect(new Point(x, 0), Child.DesiredSize));
        
        return finalSize;
    }
    
    private void UpdateScale(double targetScale)
    {
        // Round to avoid jitter on HIDPI or floating errors
        targetScale = Math.Round(targetScale, 4);

        if (Math.Abs(targetScale - _pendingScale) < 0.0001) return;

        var oldTarget = _pendingScale;
        _pendingScale = targetScale;
        
        _hysteresisTimer.Interval = TimeSpan.FromSeconds(
            Settings?.AutoScalingBufferTimeWindow ?? 5.0
        );

        // Logic
        if (targetScale < _currentScale)
        {
            // Shrinking (Content growing or Window shrinking) - Immediate
            _hysteresisTimer.Stop();
             AnimateTo(targetScale);
        }
        else
        {
            // Expanding (Content shrinking or Window growing)
            // If the change is significant or restoring to full, do it immediately?
            // User requirement: "if shrink amount (scale increase) < threshold, wait delay"
            // "If shrink amount > threshold, direct scale".
            
            bool isSignificantChange = (targetScale - _currentScale > 0.05);
            bool isRestoringToFull = (oldTarget < 1.0 && targetScale >= 0.99);
            
            if (isSignificantChange || isRestoringToFull) 
            {
                 // Significant change -> Immediate
                 _hysteresisTimer.Stop();
                 AnimateTo(targetScale);
            }
            else
            {
                // Small change -> Buffer
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

        // Animation loop - 300ms
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
            
            var easing = 1 - Math.Pow(1 - progress, 3); // CubicEaseOut
            var current = start + (end - start) * easing;
            
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = current;
            return true;
        }, TimeSpan.FromMilliseconds(16));
    }
}
