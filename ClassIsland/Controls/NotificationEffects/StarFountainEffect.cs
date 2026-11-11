using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Interfaces.Controls;
using SkiaSharp;

namespace ClassIsland.Controls.NotificationEffects;

internal sealed class StarFountainEffect : Control, INotificationEffectControl
{
    private CompositionCustomVisual? _visual;
    private bool _hasPlayed;
    private bool _isCompleted;

    private StarFountainEffect()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    internal static StarFountainEffect? Create(NotificationEffectContext _)
    {
        return IThemeService.AnimationLevel >= 1 ? new StarFountainEffect() : null;
    }

    public event EventHandler? EffectCompleted;

    public void Play()
    {
        if (_hasPlayed)
        {
            return;
        }

        _hasPlayed = true;
        if (IThemeService.AnimationLevel < 1 || ElementComposition.GetElementVisual(this)?.Compositor is not { } compositor)
        {
            Complete();
            return;
        }

        var accent = ResourceNodeExtensions.TryFindResource(this, "SystemAccentColor", out var resource) &&
                     resource is Color color
            ? color
            : Colors.DodgerBlue;
        _visual = compositor.CreateCustomVisual(new FountainVisualHandler(accent, Complete));
        _visual.Size = new Vector(Bounds.Width, Bounds.Height);
        ElementComposition.SetElementChildVisual(this, _visual);
        _visual.SendHandlerMessage(FountainVisualHandler.StartMessage);
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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_visual != null)
        {
            ReleaseVisual();
            Dispatcher.UIThread.Post(Complete);
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void Complete()
    {
        if (_isCompleted)
        {
            return;
        }

        _isCompleted = true;
        ReleaseVisual();
        EffectCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void ReleaseVisual()
    {
        if (_visual == null)
        {
            return;
        }

        _visual.SendHandlerMessage(FountainVisualHandler.StopMessage);
        ElementComposition.SetElementChildVisual(this, null);
        _visual = null;
    }

    private sealed class FountainVisualHandler(Color accent, Action completed) : CompositionCustomVisualHandler
    {
        private const float EmissionDuration = 0.4f;
        private const float MaxFlightDuration = 1.8f;
        private const int AtlasSize = 96;
        private const float AtlasCenter = AtlasSize / 2f;
        private const float StarRadius = 34;

        private static readonly TimeSpan Duration = TimeSpan.FromSeconds(EmissionDuration + MaxFlightDuration);
        private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear);

        public static readonly object StartMessage = new();
        public static readonly object StopMessage = new();

        private Particle[] _particles = [];
        private SKRect[] _sprites = [];
        private SKRotationScaleMatrix[] _transforms = [];
        private SKColor[] _colors = [];
        private SKImage? _atlas;
        private TimeSpan? _startedAt;
        private int _previousVisibleCount;

        public override void OnMessage(object message)
        {
            if (ReferenceEquals(message, StartMessage))
            {
                _startedAt = CompositionNow;
                Invalidate();
                RegisterForNextAnimationFrameUpdate();
            }
            else if (ReferenceEquals(message, StopMessage))
            {
                Stop();
            }
        }

        public override void OnAnimationFrameUpdate()
        {
            if (_startedAt is not { } startedAt)
            {
                return;
            }

            if (CompositionNow - startedAt >= Duration)
            {
                Finish();
                return;
            }

            Invalidate();
            RegisterForNextAnimationFrameUpdate();
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            var size = EffectiveSize;
            if (_startedAt is not { } startedAt || size.X <= 0 || size.Y <= 0)
            {
                return;
            }

            var leaseFeature = drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
            {
                Finish();
                return;
            }

            var width = (float)size.X;
            var height = (float)size.Y;
            if (_particles.Length == 0)
            {
                CreateParticles(width);
            }

            var elapsed = (float)(CompositionNow - startedAt).TotalSeconds;
            var visibleCount = UpdateSprites(elapsed, width, height);
            if (visibleCount == 0)
            {
                return;
            }

            _atlas ??= CreateAtlas(accent);
            using var lease = leaseFeature.Lease();
            lease.SkCanvas.DrawAtlas(_atlas, _sprites, _transforms, _colors, SKBlendMode.Modulate, Sampling, null);
        }

        private int UpdateSprites(float elapsed, float width, float height)
        {
            var visibleCount = 0;
            foreach (var particle in _particles)
            {
                var age = elapsed - particle.Delay;
                var progress = age / particle.FlightDuration;
                if (progress is < 0 or >= 1)
                {
                    continue;
                }

                var travel = particle.Distance * width * progress;
                var x = particle.FromRight ? width - travel : travel;
                var y = height - 4 * progress * (1 - progress) * particle.PeakHeight * height;
                var padding = particle.Radius * 2;
                if (x < -padding || x > width + padding || y < -padding || y > height + padding)
                {
                    continue;
                }

                var opacity = MathF.Min(1, (1 - progress) / 0.35f);
                _transforms[visibleCount] = SKRotationScaleMatrix.Create(particle.Radius / StarRadius,
                    particle.Angle + particle.Spin * age, x, y, AtlasCenter, AtlasCenter);
                _colors[visibleCount] = SKColors.White.WithAlpha((byte)(255 * opacity));
                visibleCount++;
            }

            // DrawAtlas takes whole arrays. Empty transforms make unused entries zero-area quads.
            if (visibleCount < _previousVisibleCount)
            {
                Array.Clear(_transforms, visibleCount, _previousVisibleCount - visibleCount);
            }

            _previousVisibleCount = visibleCount;
            return visibleCount;
        }

        private void CreateParticles(float width)
        {
            var particlesPerSide = (int)Math.Clamp(width / 16, 56, 128);
            var count = particlesPerSide * 2;
            _particles = new Particle[count];
            _sprites = new SKRect[count];
            _transforms = new SKRotationScaleMatrix[count];
            _colors = new SKColor[count];
            Array.Fill(_sprites, new SKRect(0, 0, AtlasSize, AtlasSize));

            var random = Random.Shared;
            for (var i = 0; i < count; i++)
            {
                _particles[i] = new Particle(
                    FromRight: i % 2 != 0,
                    Delay: (i / 2 + random.NextSingle()) / particlesPerSide * EmissionDuration,
                    FlightDuration: MaxFlightDuration - random.NextSingle() * 0.45f,
                    Distance: 0.25f + random.NextSingle() * 0.55f,
                    PeakHeight: 0.3f + random.NextSingle() * 0.4f,
                    Radius: 5 + random.NextSingle() * 8,
                    Angle: random.NextSingle() * MathF.Tau,
                    Spin: (random.NextSingle() - 0.5f) * 9);
            }
        }

        private static SKImage CreateAtlas(Color color)
        {
            using var bitmap = new SKBitmap(new SKImageInfo(AtlasSize, AtlasSize, SKColorType.Bgra8888, SKAlphaType.Premul));
            using var path = new SKPath();
            path.MoveTo(AtlasCenter, AtlasCenter - StarRadius);
            for (var i = 1; i < 10; i++)
            {
                var angle = -MathF.PI / 2 + i * MathF.PI / 5;
                var radius = i % 2 == 0 ? StarRadius : StarRadius * 0.46f;
                path.LineTo(AtlasCenter + radius * MathF.Cos(angle), AtlasCenter + radius * MathF.Sin(angle));
            }

            path.Close();
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2.5f);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 5.5f,
                StrokeJoin = SKStrokeJoin.Round,
                Color = new SKColor((byte)(color.R * 0.8f), (byte)(color.G * 0.8f), (byte)(color.B * 0.8f), 108),
                MaskFilter = blur
            };
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Save();
                canvas.Translate(2, 3);
                canvas.DrawPath(path, paint);
                canvas.Restore();

                paint.MaskFilter = null;
                paint.Color = SKColors.White;
                canvas.DrawPath(path, paint);
            }

            bitmap.SetImmutable();
            return SKImage.FromBitmap(bitmap);
        }

        private void Finish()
        {
            Stop();
            Invalidate();
            Dispatcher.UIThread.Post(completed);
        }

        private void Stop()
        {
            _startedAt = null;
            _atlas?.Dispose();
            _atlas = null;
        }

        private readonly record struct Particle(bool FromRight, float Delay, float FlightDuration, float Distance,
            float PeakHeight, float Radius, float Angle, float Spin);
    }
}
