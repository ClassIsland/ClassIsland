using System;
using System.IO;
using System.Threading;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Services;
using ColorHelper;
using SkiaSharp;
using ColorConverter = ColorHelper.ColorConverter;

namespace ClassIsland.Helpers;

internal static class AccentColorHelper
{
    private const int MaxImageSize = 640;

    internal static AccentPickResult Extract(Stream image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var codec = SKCodec.Create(image);
        if (codec == null)
            return new AccentPickResult(AccentPickStatus.Failed);

        var sourceInfo = codec.Info;
        var scale = Math.Min(1f, (float)MaxImageSize / Math.Max(sourceInfo.Width, sourceInfo.Height));
        var dimensions = codec.GetScaledDimensions(scale);
        using var colorSpace = SKColorSpace.CreateSrgb();
        var decodeInfo = new SKImageInfo(dimensions.Width, dimensions.Height, SKColorType.Rgba8888,
            sourceInfo.AlphaType, colorSpace);
        using var decoded = SKBitmap.Decode(codec, decodeInfo);
        if (decoded == null)
            return new AccentPickResult(AccentPickStatus.Failed);

        var requiresResize = Math.Max(decoded.Width, decoded.Height) > MaxImageSize;
        using var resized = requiresResize
            ? decoded.Resize(new SKImageInfo(
                    Math.Max(1, (int)Math.Round(sourceInfo.Width * scale)),
                    Math.Max(1, (int)Math.Round(sourceInfo.Height * scale)),
                    decoded.ColorType, decoded.AlphaType, colorSpace),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            : null;
        if (requiresResize && resized == null)
            return new AccentPickResult(AccentPickStatus.Failed);
        var bitmap = resized ?? decoded;
        var color = GetAccentColor(bitmap, cancellationToken);
        using var preview = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return new AccentPickResult(AccentPickStatus.Success, color, preview?.ToArray() ?? []);
    }

    internal static Color GetAccentColor(SKBitmap bitmap, CancellationToken cancellationToken)
    {
        long red = 0, green = 0, blue = 0, count = 0;
        for (var y = 0; y < bitmap.Height; y += 2)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < bitmap.Width; x += 2)
            {
                var pixel = bitmap.GetPixel(x, y);
                red += pixel.Red;
                green += pixel.Green;
                blue += pixel.Blue;
                count++;
            }
        }

        // The 1.x experimental picker used the mean RGB hue and value at a fixed saturation.
        var hsv = ColorConverter.RgbToHsv(new RGB((byte)(red / count), (byte)(green / count), (byte)(blue / count)));
        var result = ColorConverter.HsvToRgb(new HSV(hsv.H, 60, hsv.V));
        return Color.FromRgb(result.R, result.G, result.B);
    }
}
