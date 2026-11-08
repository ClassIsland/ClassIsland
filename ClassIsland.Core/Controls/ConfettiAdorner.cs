using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Controls;

internal sealed class ConfettiAdorner : Control
{
    internal static readonly RoutedEvent<RoutedEventArgs> ShowConfettiEvent =
        RoutedEvent.Register<ConfettiAdorner, RoutedEventArgs>("ShowConfetti", RoutingStrategies.Bubble);

    private TopLevel? _topLevel;
    private CompositionCustomVisual? _visual;

    public ConfettiAdorner()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(ShowConfettiEvent, OnShowConfetti);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(ShowConfettiEvent, OnShowConfetti);
        _topLevel = null;

        if (_visual != null)
        {
            _visual.SendHandlerMessage(ConfettiVisualHandler.StopMessage);
            ElementComposition.SetElementChildVisual(this, null);
            _visual = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arrangedSize = base.ArrangeOverride(finalSize);
        if (_visual != null)
        {
            _visual.Size = new Vector(arrangedSize.Width, arrangedSize.Height);
        }

        return arrangedSize;
    }

    private void OnShowConfetti(object? sender, RoutedEventArgs e)
    {
        if (IThemeService.AnimationLevel < 1)
        {
            return;
        }

        if (_visual == null && ElementComposition.GetElementVisual(this)?.Compositor is { } compositor)
        {
            _visual = compositor.CreateCustomVisual(new ConfettiVisualHandler());
            _visual.Size = new Vector(Bounds.Width, Bounds.Height);
            ElementComposition.SetElementChildVisual(this, _visual);
        }

        if (_visual != null)
        {
            _visual.SendHandlerMessage(ConfettiVisualHandler.StartMessage);
            e.Handled = true;
        }
    }

    private sealed class ConfettiVisualHandler : CompositionCustomVisualHandler
    {
        private const int MaxParticles = 96;
        private const int FadeSteps = 16;
        private const double DurationSeconds = 2;
        private const float EdgePadding = 12;

        private static readonly ImmutableSolidColorBrush[][] Brushes = CreateBrushes();

        public static readonly object StartMessage = new();
        public static readonly object StopMessage = new();

        private readonly Particle[] _particles = new Particle[MaxParticles];
        private TimeSpan? _startedAt;
        private int _particleCount;

        public override void OnMessage(object message)
        {
            if (ReferenceEquals(message, StopMessage))
            {
                _startedAt = null;
                return;
            }

            if (ReferenceEquals(message, StartMessage) && _startedAt == null)
            {
                _startedAt = CompositionNow;
                _particleCount = 0;
                Invalidate();
                RegisterForNextAnimationFrameUpdate();
            }
        }

        public override void OnAnimationFrameUpdate()
        {
            if (_startedAt is not { } startedAt)
            {
                return;
            }

            Invalidate();
            if ((CompositionNow - startedAt).TotalSeconds < DurationSeconds)
            {
                RegisterForNextAnimationFrameUpdate();
            }
            else
            {
                _startedAt = null;
            }
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            var size = EffectiveSize;
            if (_startedAt is not { } startedAt || size.X <= 0 || size.Y <= 0)
            {
                return;
            }

            var width = (float)size.X;
            var height = (float)size.Y;
            if (_particleCount == 0)
            {
                CreateParticles(width);
            }

            var elapsed = (float)(CompositionNow - startedAt).TotalSeconds;
            for (var i = 0; i < _particleCount; i++)
            {
                var particle = _particles[i];
                var age = elapsed - particle.Delay;
                var progress = age / particle.FallDuration;
                if (progress is < 0 or >= 1)
                {
                    continue;
                }

                var roll = particle.Phase + age * 12;
                var x = particle.Origin * width + particle.Drift * age + MathF.Sin(roll) * 9;
                var y = (height + 2 * EdgePadding) * progress * (0.45f + 0.55f * progress) - EdgePadding;
                var halfWidth = particle.Size * (0.12f + 0.38f * MathF.Abs(MathF.Cos(roll)));
                var halfHeight = particle.Size * 0.65f;
                var opacity = MathF.Min(1, (1 - progress) * 5);
                var brush = Brushes[particle.ColorIndex][(int)(opacity * FadeSteps)];

                var rotation = Matrix.CreateRotation(particle.Angle + age * particle.Spin, new Point(x, y));
                using var transform = drawingContext.PushPreTransform(rotation);
                drawingContext.FillRectangle(brush, new Rect(x - halfWidth, y - halfHeight, 2 * halfWidth, 2 * halfHeight));
            }
        }

        private void CreateParticles(float width)
        {
            _particleCount = (int)Math.Clamp(width / 9, 12, MaxParticles);
            var random = Random.Shared;
            for (var i = 0; i < _particleCount; i++)
            {
                _particles[i] = new Particle(
                    Origin: (i + random.NextSingle()) / _particleCount,
                    Delay: random.NextSingle() * 0.3f,
                    FallDuration: 1.25f + random.NextSingle() * 0.45f,
                    Drift: (random.NextSingle() - 0.5f) * 90,
                    Phase: random.NextSingle() * MathF.Tau,
                    Angle: random.NextSingle() * MathF.Tau,
                    Spin: (random.NextSingle() - 0.5f) * 3 * MathF.PI,
                    Size: 4 + random.NextSingle() * 5,
                    ColorIndex: random.Next(Brushes.Length));
            }
        }

        private static ImmutableSolidColorBrush[][] CreateBrushes()
        {
            Color[] colors =
            [
                Color.FromRgb(244, 96, 135), Color.FromRgb(255, 171, 71), Color.FromRgb(249, 213, 84),
                Color.FromRgb(49, 192, 157), Color.FromRgb(66, 155, 242), Color.FromRgb(160, 117, 242)
            ];

            var brushes = new ImmutableSolidColorBrush[colors.Length][];
            for (var i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                brushes[i] = new ImmutableSolidColorBrush[FadeSteps + 1];
                for (var step = 0; step <= FadeSteps; step++)
                {
                    var alpha = (byte)(255 * step / FadeSteps);
                    brushes[i][step] = new ImmutableSolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
                }
            }

            return brushes;
        }

        private readonly record struct Particle(float Origin, float Delay, float FallDuration, float Drift,
            float Phase, float Angle, float Spin, float Size, int ColorIndex);
    }
}
