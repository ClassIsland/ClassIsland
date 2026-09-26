using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using SkiaSharp;

namespace ClassIsland.Core.Controls;

/// <summary>
/// A three-shape loading indicator inspired by Material 3 Expressive motion.
/// </summary>
public class ExpressiveLoadingIndicator : TemplatedControl
{
    private CompositionCustomVisual? _customVisual;

    /// <summary>
    /// Defines the <see cref="IsActive"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ExpressiveLoadingIndicator, bool>(nameof(IsActive), true);

    /// <summary>
    /// Gets or sets whether the loading animation is running.
    /// </summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(ExpressiveLoadingIndicator);

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachCustomVisual();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachCustomVisual();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var arrangedSize = base.ArrangeOverride(finalSize);
        if (_customVisual != null)
        {
            _customVisual.Size = new Vector(arrangedSize.Width, arrangedSize.Height);
        }

        return arrangedSize;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ForegroundProperty)
        {
            SendForeground();
        }
        else if (change.Property == IsActiveProperty || change.Property == IsVisibleProperty)
        {
            UpdateAnimationState();
        }
    }

    private void AttachCustomVisual()
    {
        var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
        if (compositor == null)
        {
            return;
        }

        DetachCustomVisual();

        _customVisual = compositor.CreateCustomVisual(new ExpressiveLoadingVisualHandler());
        _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        ElementComposition.SetElementChildVisual(this, _customVisual);

        SendForeground();
        UpdateAnimationState();
    }

    private void DetachCustomVisual()
    {
        if (_customVisual == null)
        {
            return;
        }

        _customVisual.SendHandlerMessage(ExpressiveLoadingVisualHandler.DisposeMessage);
        ElementComposition.SetElementChildVisual(this, null);
        _customVisual = null;
    }

    private void SendForeground()
    {
        if (_customVisual == null)
        {
            return;
        }

        var color = Foreground is ISolidColorBrush brush
            ? ApplyOpacity(brush.Color, brush.Opacity)
            : Colors.DodgerBlue;
        _customVisual.SendHandlerMessage(new ExpressiveLoadingVisualHandler.ColorMessage(color));
    }

    private void UpdateAnimationState()
    {
        if (_customVisual == null)
        {
            return;
        }

        if (IThemeService.AnimationLevel < 1)
        {
            _customVisual.SendHandlerMessage(ExpressiveLoadingVisualHandler.ShowStaticMessage);
        }
        else if (IsActive && IsVisible)
        {
            _customVisual.SendHandlerMessage(ExpressiveLoadingVisualHandler.StartMessage);
        }
        else
        {
            _customVisual.SendHandlerMessage(ExpressiveLoadingVisualHandler.PauseMessage);
        }
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(color.A * opacity), byte.MinValue, byte.MaxValue);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private sealed class ExpressiveLoadingVisualHandler : CompositionCustomVisualHandler
    {
        private const int ShapeCount = 3;
        private const int SampleCount = 60;
        private const double ShapeSize = 36;
        private const double ShapeGap = 12;
        private const double ShapeCornerRadius = 4;
        private const double ShapeCornerRadiusScale = 2 * ShapeCornerRadius / ShapeSize;
        private const double MorphDurationMs = 560;
        private const double RotationDurationMs = 900;
        private const double RotationReboundDurationMs = 180;
        private const double StagePauseDurationMs = 300;
        private const double StageDurationMs =
            RotationDurationMs + RotationReboundDurationMs + StagePauseDurationMs;
        private const double LoopDurationMs = StageDurationMs * ShapeCount;
        private const double ShapeDelayMs = 160;
        private const double RotationReboundDegrees = 10;
        private const double RestingOpacity = 0.58;
        private const double MaxSpringProgress = 1.0325;
        private const double MaxMorphScale = 1 + 2 * (MaxSpringProgress - 1);
        private static readonly double SpringEndValue = RawSpring(1);
        private static readonly Point[][] ShapePoints = CreateAlignedShapePoints();

        public static readonly object StartMessage = new();
        public static readonly object PauseMessage = new();
        public static readonly object ShowStaticMessage = new();
        public static readonly object DisposeMessage = new();

        public readonly record struct ColorMessage(Color Color);

        private TimeSpan _animationElapsed;
        private TimeSpan? _lastCompositionTime;
        private Color _color = Colors.DodgerBlue;
        private bool _isRunning;
        private bool _showStaticShapes = true;
        private bool _isRenderingSupported = true;
        private bool _isDisposed;
        private SKPath? _path;
        private SKPaint? _paint;
        private readonly SKPoint[] _transformedPoints = new SKPoint[SampleCount];
        private readonly SKPoint[] _cornerPoints = new SKPoint[SampleCount];
        private readonly SKPoint[] _cornerEntries = new SKPoint[SampleCount];
        private readonly SKPoint[] _cornerExits = new SKPoint[SampleCount];

        /// <inheritdoc />
        public override void OnMessage(object message)
        {
            if (_isDisposed)
            {
                return;
            }

            switch (message)
            {
                case ColorMessage colorMessage:
                    _color = colorMessage.Color;
                    Invalidate();
                    break;
                default:
                    HandleCommand(message);
                    break;
            }
        }

        /// <inheritdoc />
        public override void OnAnimationFrameUpdate()
        {
            if (!_isRunning || !_isRenderingSupported || _isDisposed)
            {
                return;
            }

            AdvanceClock();
            Invalidate();
            RegisterForNextAnimationFrameUpdate();
        }

        /// <inheritdoc />
        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            if (_isDisposed || EffectiveSize.X <= 0 || EffectiveSize.Y <= 0)
            {
                return;
            }

            var leaseFeature = drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
            {
                _isRenderingSupported = false;
                _isRunning = false;
                _lastCompositionTime = null;
                return;
            }

            EnsureDrawingResources();
            using var lease = leaseFeature.Lease();
            DrawShapes(lease.SkCanvas);
        }

        private void HandleCommand(object message)
        {
            if (ReferenceEquals(message, StartMessage))
            {
                if (_isRunning || !_isRenderingSupported)
                {
                    return;
                }

                _showStaticShapes = false;
                _isRunning = true;
                _lastCompositionTime = CompositionNow;
                Invalidate();
                RegisterForNextAnimationFrameUpdate();
            }
            else if (ReferenceEquals(message, PauseMessage))
            {
                AdvanceClock();
                _isRunning = false;
                _lastCompositionTime = null;
            }
            else if (ReferenceEquals(message, ShowStaticMessage))
            {
                _isRunning = false;
                _lastCompositionTime = null;
                _showStaticShapes = true;
                Invalidate();
            }
            else if (ReferenceEquals(message, DisposeMessage))
            {
                _isRunning = false;
                _isDisposed = true;
                _lastCompositionTime = null;
                _path?.Dispose();
                _paint?.Dispose();
                _path = null;
                _paint = null;
            }
        }

        private void AdvanceClock()
        {
            if (!_isRunning)
            {
                return;
            }

            var now = CompositionNow;
            if (_lastCompositionTime is { } lastCompositionTime)
            {
                _animationElapsed += now - lastCompositionTime;
            }

            _lastCompositionTime = now;
        }

        private void EnsureDrawingResources()
        {
            _path ??= new SKPath();
            _paint ??= new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
        }

        private void DrawShapes(SKCanvas canvas)
        {
            var cellWidth = EffectiveSize.X / ShapeCount;
            var centerSpacing = Math.Min(ShapeSize + ShapeGap, cellWidth);
            var groupStartX = (EffectiveSize.X - centerSpacing * (ShapeCount - 1)) / 2;
            var availableRadius = Math.Min(EffectiveSize.Y, cellWidth) / (2 * MaxMorphScale);
            var radius = Math.Min(ShapeSize / 2, availableRadius);
            if (radius <= 0 || _path == null || _paint == null)
            {
                return;
            }

            var elapsedMs = _animationElapsed.TotalMilliseconds;

            for (var shapeIndex = 0; shapeIndex < ShapeCount; shapeIndex++)
            {
                var centerX = groupStartX + centerSpacing * shapeIndex;
                var centerY = EffectiveSize.Y * 0.5;

                if (_showStaticShapes)
                {
                    DrawPolygon(canvas, ShapePoints[shapeIndex], ShapePoints[shapeIndex], 0, 0,
                        centerX, centerY, radius, RestingOpacity);
                    continue;
                }

                var delayedElapsedMs = elapsedMs - shapeIndex * ShapeDelayMs;
                if (delayedElapsedMs < 0)
                {
                    DrawPolygon(canvas, ShapePoints[0], ShapePoints[0], 0, 0,
                        centerX, centerY, radius, RestingOpacity);
                    continue;
                }

                var loopTime = delayedElapsedMs % LoopDurationMs;
                var stageIndex = (int)(loopTime / StageDurationMs);
                var stageTime = loopTime - stageIndex * StageDurationMs;
                var from = ShapePoints[stageIndex];
                var to = ShapePoints[(stageIndex + 1) % ShapeCount];
                var morphProgress = stageTime < MorphDurationMs
                    ? Spring(stageTime / MorphDurationMs)
                    : 1;
                var rotation = CalculateRotation(stageTime);
                var opacity = CalculateMorphOpacity(stageTime);

                DrawPolygon(canvas, from, to, morphProgress, rotation,
                    centerX, centerY, radius, opacity);
            }
        }

        private void DrawPolygon(
            SKCanvas canvas,
            Point[] from,
            Point[] to,
            double progress,
            double rotationDegrees,
            double centerX,
            double centerY,
            double radius,
            double opacity)
        {
            if (_path == null || _paint == null)
            {
                return;
            }

            var alpha = (byte)Math.Clamp(Math.Round(_color.A * opacity), byte.MinValue, byte.MaxValue);
            _paint.Color = new SKColor(_color.R, _color.G, _color.B, alpha);
            var radians = rotationDegrees * Math.PI / 180;
            var cosine = Math.Cos(radians);
            var sine = Math.Sin(radians);

            for (var pointIndex = 0; pointIndex < SampleCount; pointIndex++)
            {
                var source = from[pointIndex];
                var target = to[pointIndex];
                var x = source.X + (target.X - source.X) * progress;
                var y = source.Y + (target.Y - source.Y) * progress;
                var rotatedX = (x * cosine - y * sine) * radius + centerX;
                var rotatedY = (x * sine + y * cosine) * radius + centerY;
                _transformedPoints[pointIndex] = new SKPoint((float)rotatedX, (float)rotatedY);
            }

            var cornerCount = ExtractCornerPoints(_transformedPoints, _cornerPoints);
            BuildRoundedPath(cornerCount, (float)(radius * ShapeCornerRadiusScale));
            canvas.DrawPath(_path, _paint);
        }

        private static int ExtractCornerPoints(SKPoint[] points, SKPoint[] corners)
        {
            var cornerCount = 0;
            for (var pointIndex = 0; pointIndex < SampleCount; pointIndex++)
            {
                var previous = points[(pointIndex + SampleCount - 1) % SampleCount];
                var current = points[pointIndex];
                var next = points[(pointIndex + 1) % SampleCount];
                var incomingX = current.X - previous.X;
                var incomingY = current.Y - previous.Y;
                var outgoingX = next.X - current.X;
                var outgoingY = next.Y - current.Y;
                var lengthProduct = Math.Sqrt(
                    (incomingX * incomingX + incomingY * incomingY) *
                    (outgoingX * outgoingX + outgoingY * outgoingY));
                if (lengthProduct <= float.Epsilon)
                {
                    continue;
                }

                var normalizedCross = Math.Abs(incomingX * outgoingY - incomingY * outgoingX) /
                                      lengthProduct;
                if (normalizedCross <= 0.001)
                {
                    continue;
                }

                corners[cornerCount++] = current;
            }

            return cornerCount;
        }

        private void BuildRoundedPath(int cornerCount, float cornerRadius)
        {
            if (_path == null)
            {
                return;
            }

            _path.Reset();
            if (cornerCount < 3)
            {
                _path.MoveTo(_transformedPoints[0]);
                for (var pointIndex = 1; pointIndex < SampleCount; pointIndex++)
                {
                    _path.LineTo(_transformedPoints[pointIndex]);
                }

                _path.Close();
                return;
            }

            for (var cornerIndex = 0; cornerIndex < cornerCount; cornerIndex++)
            {
                var previous = _cornerPoints[(cornerIndex + cornerCount - 1) % cornerCount];
                var current = _cornerPoints[cornerIndex];
                var next = _cornerPoints[(cornerIndex + 1) % cornerCount];
                var previousX = previous.X - current.X;
                var previousY = previous.Y - current.Y;
                var nextX = next.X - current.X;
                var nextY = next.Y - current.Y;
                var previousLength = Math.Sqrt(previousX * previousX + previousY * previousY);
                var nextLength = Math.Sqrt(nextX * nextX + nextY * nextY);
                if (previousLength <= float.Epsilon || nextLength <= float.Epsilon)
                {
                    _cornerEntries[cornerIndex] = current;
                    _cornerExits[cornerIndex] = current;
                    continue;
                }

                var previousUnitX = previousX / previousLength;
                var previousUnitY = previousY / previousLength;
                var nextUnitX = nextX / nextLength;
                var nextUnitY = nextY / nextLength;
                var cornerAngle = Math.Acos(Math.Clamp(
                    previousUnitX * nextUnitX + previousUnitY * nextUnitY, -1, 1));
                var tangentDistance = cornerRadius / Math.Tan(cornerAngle / 2);
                var maximumTangentDistance = Math.Min(previousLength, nextLength) * 0.45;
                tangentDistance = Math.Clamp(tangentDistance, 0, maximumTangentDistance);
                _cornerEntries[cornerIndex] = new SKPoint(
                    (float)(current.X + previousUnitX * tangentDistance),
                    (float)(current.Y + previousUnitY * tangentDistance));
                _cornerExits[cornerIndex] = new SKPoint(
                    (float)(current.X + nextUnitX * tangentDistance),
                    (float)(current.Y + nextUnitY * tangentDistance));
            }

            _path.MoveTo(_cornerExits[cornerCount - 1]);
            for (var cornerIndex = 0; cornerIndex < cornerCount; cornerIndex++)
            {
                _path.LineTo(_cornerEntries[cornerIndex]);
                _path.QuadTo(_cornerPoints[cornerIndex], _cornerExits[cornerIndex]);
            }

            _path.Close();
        }

        private static double CalculateRotation(double stageTime)
        {
            if (stageTime < RotationDurationMs)
            {
                var progress = stageTime / RotationDurationMs;
                return (360 + RotationReboundDegrees) * EaseOutQuadratic(progress);
            }

            var reboundTime = stageTime - RotationDurationMs;
            if (reboundTime < RotationReboundDurationMs)
            {
                var progress = reboundTime / RotationReboundDurationMs;
                return 360 + RotationReboundDegrees * (1 - SmoothStep(progress));
            }

            return 0;
        }

        private static double CalculateMorphOpacity(double stageTime)
        {
            if (stageTime >= MorphDurationMs)
            {
                return RestingOpacity;
            }

            var progress = Math.Clamp(stageTime / MorphDurationMs, 0, 1);
            var emphasisProgress = Math.Pow(Math.Sin(Math.PI * progress), 2);
            return RestingOpacity + (1 - RestingOpacity) * emphasisProgress;
        }

        private static double Spring(double progress)
        {
            var normalized = RawSpring(Math.Clamp(progress, 0, 1)) / SpringEndValue;
            return Math.Clamp(normalized, 0, MaxSpringProgress);
        }

        private static double RawSpring(double progress) =>
            1 - Math.Exp(-7 * progress) *
            (Math.Cos(10 * progress) + 0.7 * Math.Sin(10 * progress));

        private static double EaseOutQuadratic(double progress) => 1 - Math.Pow(1 - progress, 2);

        private static double SmoothStep(double progress) => progress * progress * (3 - 2 * progress);

        private static Point[][] CreateAlignedShapePoints()
        {
            var square = SampleRegularPolygon(4, -135);
            var triangle = SampleRegularPolygon(3, -90);
            var pentagon = SampleRegularPolygon(5, -90);
            var bestTriangleShift = 0;
            var bestPentagonShift = 0;
            var bestDistance = double.MaxValue;

            for (var triangleShift = 0; triangleShift < SampleCount; triangleShift++)
            {
                for (var pentagonShift = 0; pentagonShift < SampleCount; pentagonShift++)
                {
                    var distance = CalculateDistance(square, triangle, 0, triangleShift) +
                                   CalculateDistance(triangle, pentagon, triangleShift, pentagonShift) +
                                   CalculateDistance(pentagon, square, pentagonShift, 0);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    bestTriangleShift = triangleShift;
                    bestPentagonShift = pentagonShift;
                }
            }

            return
            [
                square,
                ShiftPoints(triangle, bestTriangleShift),
                ShiftPoints(pentagon, bestPentagonShift)
            ];
        }

        private static Point[] SampleRegularPolygon(int sideCount, double rotationDegrees)
        {
            var vertices = new Point[sideCount];
            var rotationRadians = rotationDegrees * Math.PI / 180;
            for (var vertexIndex = 0; vertexIndex < sideCount; vertexIndex++)
            {
                var angle = rotationRadians + vertexIndex * Math.PI * 2 / sideCount;
                vertices[vertexIndex] = new Point(Math.Cos(angle), Math.Sin(angle));
            }

            var points = new Point[SampleCount];
            for (var pointIndex = 0; pointIndex < SampleCount; pointIndex++)
            {
                var edgePosition = pointIndex * sideCount / (double)SampleCount;
                var edgeIndex = (int)Math.Floor(edgePosition);
                var edgeProgress = edgePosition - edgeIndex;
                var start = vertices[edgeIndex];
                var end = vertices[(edgeIndex + 1) % sideCount];
                points[pointIndex] = new Point(
                    start.X + (end.X - start.X) * edgeProgress,
                    start.Y + (end.Y - start.Y) * edgeProgress);
            }

            return points;
        }

        private static double CalculateDistance(Point[] first, Point[] second, int firstShift, int secondShift)
        {
            var distance = 0d;
            for (var pointIndex = 0; pointIndex < SampleCount; pointIndex++)
            {
                var firstPoint = first[(pointIndex + firstShift) % SampleCount];
                var secondPoint = second[(pointIndex + secondShift) % SampleCount];
                var deltaX = firstPoint.X - secondPoint.X;
                var deltaY = firstPoint.Y - secondPoint.Y;
                distance += deltaX * deltaX + deltaY * deltaY;
            }

            return distance;
        }

        private static Point[] ShiftPoints(Point[] points, int shift)
        {
            var shifted = new Point[SampleCount];
            for (var pointIndex = 0; pointIndex < SampleCount; pointIndex++)
            {
                shifted[pointIndex] = points[(pointIndex + shift) % SampleCount];
            }

            return shifted;
        }
    }
}
