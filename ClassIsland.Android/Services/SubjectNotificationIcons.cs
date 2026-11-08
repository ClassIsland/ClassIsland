using Android.Graphics;
using ClassIsland.Core.Enums.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using AvaloniaColor = Avalonia.Media.Color;

namespace ClassIsland.Android.Services;

internal sealed class SubjectNotificationIcons(Bitmap monochrome, Bitmap color, Bitmap? progressEnd) : IDisposable
{
    public Bitmap Monochrome { get; } = monochrome;

    public Bitmap Color { get; } = color;

    public Bitmap? ProgressEnd { get; } = progressEnd;

    public static async Task<SubjectNotificationIcons> RenderAsync(
        string iconExpression,
        string colorHex,
        string? progressEndIconExpression,
        string? progressEndColorHex,
        CancellationToken cancellationToken)
    {
        var themeColor = ParseColor(colorHex);
        using var monochrome = await IconBitmapHelper.RenderAsync(iconExpression,
            IconBitmapRenderMode.Monochrome, monochromeColor: themeColor,
            size: 64, cancellationToken: cancellationToken);
        using var color = await IconBitmapHelper.RenderAsync(iconExpression,
            foreground: themeColor, size: 128, cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var nativeMonochrome = ToNativeBitmap(monochrome);
        Bitmap? nativeColor = null;
        Bitmap? progressEnd = null;
        try
        {
            nativeColor = ToNativeBitmap(color);
            if (progressEndIconExpression != null)
            {
                try
                {
                    using var endBitmap = await IconBitmapHelper.RenderAsync(progressEndIconExpression,
                        IconBitmapRenderMode.Monochrome, monochromeColor: ParseColor(progressEndColorHex),
                        size: 64, cancellationToken: cancellationToken);
                    progressEnd = ToNativeBitmap(endBitmap);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    IAppHost.GetService<ILogger<SubjectNotificationIcons>>()
                        .LogWarning(exception, "无法渲染下节课进度图标");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new SubjectNotificationIcons(nativeMonochrome, nativeColor, progressEnd);
        }
        catch
        {
            nativeMonochrome.Dispose();
            nativeColor?.Dispose();
            progressEnd?.Dispose();
            throw;
        }
    }

    public static int GetThemeColor(string? colorHex)
    {
        var color = ParseColor(colorHex);
        return unchecked((int)((uint)color.A << 24 | (uint)color.R << 16 |
                               (uint)color.G << 8 | color.B));
    }

    private static AvaloniaColor ParseColor(string? colorHex) =>
        AvaloniaColor.TryParse(colorHex, out var color)
            ? color
            : AvaloniaColor.Parse(Subject.Empty.ColorHex);

    private static Bitmap ToNativeBitmap(AvaloniaBitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;
        return BitmapFactory.DecodeStream(stream)
               ?? throw new InvalidOperationException("无法将科目图标转换为 Android 位图。");
    }

    public void Dispose()
    {
        Monochrome.Dispose();
        Color.Dispose();
        ProgressEnd?.Dispose();
    }
}
