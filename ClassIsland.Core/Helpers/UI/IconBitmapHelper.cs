using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassIsland.Core.Controls.IconSources;
using ClassIsland.Core.Enums.UI;
using FluentAvalonia.UI.Controls;
using SkiaSharp;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// 将图标表达式或图标源渲染为位图的辅助类。
/// </summary>
public static class IconBitmapHelper
{
    private static readonly TimeSpan ImageLoadTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 将应用内置的图标表达式渲染为位图。
    /// </summary>
    /// <param name="iconExpression">已注册处理器支持的图标表达式。</param>
    /// <param name="mode">彩色或透明背景灰度模式。</param>
    /// <param name="size">正方形位图的像素边长，必须大于零。</param>
    /// <param name="cancellationToken">取消本次渲染或图片等待的令牌。</param>
    /// <returns>固定 96 DPI 的位图，由调用者负责释放。</returns>
    /// <remarks>
    /// 调用前需初始化 Avalonia 并注册图标表达式处理器。可以从后台线程调用。
    /// 图片等待最多 30 秒；取消等待不会中断共享图片加载器的底层下载。
    /// </remarks>
    /// <example>
    /// <code>
    /// using var bitmap = await IconBitmapHelper.RenderAsync($"fluent({FluentIcons.IconsRegular})");
    /// bitmap.Save("icon.png");
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">表达式为空或参数无效。</exception>
    /// <exception cref="InvalidOperationException">表达式解析或图片加载失败。</exception>
    /// <exception cref="TimeoutException">图片加载超过 30 秒。</exception>
    /// <exception cref="OperationCanceledException">调用已取消。</exception>
    public static Task<Bitmap> RenderAsync(string iconExpression,
        IconBitmapRenderMode mode = IconBitmapRenderMode.Color, int size = 64,
        CancellationToken cancellationToken = default) =>
        RenderAsync(iconExpression, mode, null, size, cancellationToken);

    /// <summary>
    /// 将图标表达式渲染为位图，并在单色模式下保留灰度明暗着色。
    /// </summary>
    /// <param name="iconExpression">已注册处理器支持的图标表达式。</param>
    /// <param name="mode">图标的渲染模式。</param>
    /// <param name="monochromeColor">单色模式的深色端颜色；灰度越亮越接近白色，其透明度与图标透明度相乘。传入 null 输出灰度图，彩色模式忽略此参数。</param>
    /// <param name="size">正方形位图的像素边长，必须大于零。</param>
    /// <param name="cancellationToken">取消本次渲染或图片等待的令牌。</param>
    /// <returns>固定 96 DPI 的位图，由调用者负责释放。</returns>
    /// <remarks>初始化要求、图片超时和异常与不指定颜色的重载相同。</remarks>
    /// <example>
    /// <code>
    /// using var bitmap = await IconBitmapHelper.RenderAsync(
    ///     iconExpression, IconBitmapRenderMode.Monochrome, monochromeColor: Colors.Blue);
    /// </code>
    /// </example>
    public static Task<Bitmap> RenderAsync(string iconExpression,
        IconBitmapRenderMode mode, Color? monochromeColor, int size = 64,
        CancellationToken cancellationToken = default) =>
        RenderCoreAsync(iconExpression, mode, monochromeColor, size, cancellationToken, null);

    /// <summary>
    /// 将图标表达式渲染为位图，并为彩色模式的文字图标设置前景色。
    /// </summary>
    /// <param name="iconExpression">已注册处理器支持的图标表达式。</param>
    /// <param name="foreground">文字图标的前景色，通过 TextElement.Foreground 附加属性设置；传入 null 使用原前景色。图片图标和单色模式忽略此参数。</param>
    /// <param name="mode">图标的渲染模式，默认为彩色。</param>
    /// <param name="size">正方形位图的像素边长，必须大于零。</param>
    /// <param name="cancellationToken">取消本次渲染或图片等待的令牌。</param>
    /// <returns>固定 96 DPI 的位图，由调用者负责释放。</returns>
    /// <remarks>初始化要求、图片超时和异常与不指定前景色的重载相同。</remarks>
    /// <example>
    /// <code>
    /// using var bitmap = await IconBitmapHelper.RenderAsync(iconExpression, foreground: Colors.Blue);
    /// </code>
    /// </example>
    public static Task<Bitmap> RenderAsync(string iconExpression, Color? foreground,
        IconBitmapRenderMode mode = IconBitmapRenderMode.Color, int size = 64,
        CancellationToken cancellationToken = default) =>
        RenderCoreAsync(iconExpression, mode, null, size, cancellationToken, foreground);

    private static async Task<Bitmap> RenderCoreAsync(string iconExpression, IconBitmapRenderMode mode,
        Color? monochromeColor, int size, CancellationToken cancellationToken, Color? foreground)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconExpression);
        ValidateParameters(mode, size);
        cancellationToken.ThrowIfCancellationRequested();
        var source = await Dispatcher.UIThread.InvokeAsync(() => IconExpressionHelper.Parse(iconExpression),
            DispatcherPriority.Normal, cancellationToken);
        return await RenderCoreAsync(source, mode, monochromeColor, size, cancellationToken, foreground);
    }

    /// <summary>
    /// 将图标源渲染为位图，不修改图标源或应用主题。
    /// </summary>
    /// <param name="iconSource">要渲染的图标源。</param>
    /// <param name="mode">彩色或透明背景灰度模式。</param>
    /// <param name="size">正方形位图的像素边长，必须大于零。</param>
    /// <param name="cancellationToken">取消本次渲染或图片等待的令牌。</param>
    /// <returns>固定 96 DPI 的位图，由调用者负责释放。</returns>
    /// <remarks>
    /// 调用前需初始化 Avalonia。可以从后台线程调用。
    /// 图片等待最多 30 秒；取消等待不会中断共享图片加载器的底层下载。
    /// </remarks>
    /// <exception cref="ArgumentException">图标源为空或参数无效。</exception>
    /// <exception cref="InvalidOperationException">图片加载失败。</exception>
    /// <exception cref="TimeoutException">图片加载超过 30 秒。</exception>
    /// <exception cref="OperationCanceledException">调用已取消。</exception>
    public static Task<Bitmap> RenderAsync(FAIconSource iconSource,
        IconBitmapRenderMode mode = IconBitmapRenderMode.Color, int size = 64,
        CancellationToken cancellationToken = default) =>
        RenderAsync(iconSource, mode, null, size, cancellationToken);

    /// <summary>
    /// 将图标源渲染为位图，并在单色模式下保留灰度明暗着色。
    /// </summary>
    /// <param name="iconSource">要渲染的图标源。</param>
    /// <param name="mode">图标的渲染模式。</param>
    /// <param name="monochromeColor">单色模式的深色端颜色；灰度越亮越接近白色，其透明度与图标透明度相乘。传入 null 输出灰度图，彩色模式忽略此参数。</param>
    /// <param name="size">正方形位图的像素边长，必须大于零。</param>
    /// <param name="cancellationToken">取消本次渲染或图片等待的令牌。</param>
    /// <returns>固定 96 DPI 的位图，由调用者负责释放。</returns>
    /// <remarks>初始化要求、图片超时和异常与不指定颜色的重载相同；不会修改图标源。</remarks>
    public static Task<Bitmap> RenderAsync(FAIconSource iconSource,
        IconBitmapRenderMode mode, Color? monochromeColor, int size = 64,
        CancellationToken cancellationToken = default) =>
        RenderCoreAsync(iconSource, mode, monochromeColor, size, cancellationToken, null);

    /// <summary>
    /// 将图标源渲染为位图，并为彩色模式的文字图标设置前景色，不修改图标源。
    /// </summary>
    /// <param name="iconSource">要渲染的图标源。</param>
    /// <param name="foreground">文字图标的前景色，通过 TextElement.Foreground 附加属性设置；传入 null 使用原前景色。图片图标和单色模式忽略此参数。</param>
    /// <param name="mode">图标的渲染模式，默认为彩色。</param>
    /// <param name="size">正方形位图的像素边长，必须大于零。</param>
    /// <param name="cancellationToken">取消本次渲染或图片等待的令牌。</param>
    /// <returns>固定 96 DPI 的位图，由调用者负责释放。</returns>
    /// <remarks>初始化要求、图片超时和异常与不指定前景色的重载相同。</remarks>
    public static Task<Bitmap> RenderAsync(FAIconSource iconSource, Color? foreground,
        IconBitmapRenderMode mode = IconBitmapRenderMode.Color, int size = 64,
        CancellationToken cancellationToken = default) =>
        RenderCoreAsync(iconSource, mode, null, size, cancellationToken, foreground);

    private static async Task<Bitmap> RenderCoreAsync(FAIconSource iconSource, IconBitmapRenderMode mode,
        Color? monochromeColor, int size, CancellationToken cancellationToken, Color? foreground)
    {
        ArgumentNullException.ThrowIfNull(iconSource);
        ValidateParameters(mode, size);
        cancellationToken.ThrowIfCancellationRequested();

        var imageUri = await Dispatcher.UIThread.InvokeAsync(
            () => (iconSource as AdvancedImageIconSource)?.Uri, DispatcherPriority.Normal, cancellationToken);
        Bitmap? image = null;
        if (iconSource is AdvancedImageIconSource)
        {
            if (string.IsNullOrWhiteSpace(imageUri))
            {
                throw new InvalidOperationException("图标图片地址为空。");
            }

            try
            {
                image = await ImageLoader.AsyncImageLoader.ProvideImageAsync(imageUri)
                    .WaitAsync(ImageLoadTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or TimeoutException))
            {
                throw new InvalidOperationException($"无法加载图标图片：{imageUri}", exception);
            }
            if (image == null)
            {
                throw new InvalidOperationException($"无法加载图标图片：{imageUri}");
            }
        }

        // 加载器可能缓存并共享图片，只有新生成的位图归本次调用所有。
        return await Dispatcher.UIThread.InvokeAsync(() => Render(iconSource, image, mode, size, monochromeColor, foreground),
            DispatcherPriority.Normal, cancellationToken);
    }

    private static void ValidateParameters(IconBitmapRenderMode mode, int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        if (mode is not (IconBitmapRenderMode.Color or IconBitmapRenderMode.Monochrome))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private static Bitmap Render(FAIconSource source, Bitmap? image, IconBitmapRenderMode mode, int size,
        Color? monochromeColor, Color? foreground)
    {
        var icon = new FAIconSourceElement
        {
            IconSource = image == null ? source : new FAImageIconSource { Source = image },
            Foreground = source.Foreground ?? Brushes.Black
        };
        var viewbox = new Viewbox
        {
            Child = icon,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both
        };
        var border = new Border
        {
            Child = viewbox,
            Background = mode == IconBitmapRenderMode.Color ? Brushes.White : null,
            Padding = mode == IconBitmapRenderMode.Color ? new Thickness(size / 8.0) : default,
            CornerRadius = mode == IconBitmapRenderMode.Color ? new CornerRadius(size * (3 / 16.0)) : default
        };
        var scope = new ThemeVariantScope
        {
            RequestedThemeVariant = ThemeVariant.Light,
            Child = border,
            Width = size,
            Height = size
        };
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(96, 96));
        try
        {
            scope.BeginInit();
            scope.EndInit();
            scope.Measure(new Size(size, size));
            if (mode == IconBitmapRenderMode.Color && foreground is { } color)
            {
                var brush = new SolidColorBrush(color);
                foreach (var control in icon.GetVisualDescendants().OfType<Control>())
                {
                    if (control is FAFontIcon or FASymbolIcon or TextBlock)
                    {
                        control.SetValue(TextElement.ForegroundProperty, brush);
                    }
                }
            }
            scope.Arrange(new Rect(0, 0, size, size));
            bitmap.Render(scope);
            if (mode == IconBitmapRenderMode.Color)
            {
                return bitmap;
            }

            var grayscale = ConvertToMonochrome(bitmap, monochromeColor);
            bitmap.Dispose();
            return grayscale;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
        finally
        {
            // 图标工厂会绑定图标源的属性，需解除临时控件的绑定，避免图标源保留这些控件。
            var controls = icon.GetVisualDescendants().OfType<Control>().Prepend(icon).ToArray();
            icon.IconSource = null;
            viewbox.Child = null;
            border.Child = null;
            scope.Child = null;
            foreach (var control in controls)
            {
                if (control is FABitmapIcon bitmapIcon)
                {
                    UnlinkFromBitmapIconSource(bitmapIcon);
                    GetBitmap(bitmapIcon) = null;
                }
                foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(control))
                {
                    // 这两个属性由位图源直接管理，未创建本地绑定。
                    if (control is FABitmapIcon &&
                        (property == FABitmapIcon.UriSourceProperty || property == FABitmapIcon.ShowAsMonochromeProperty))
                    {
                        continue;
                    }
                    if (!property.IsDirect && !property.IsReadOnly)
                    {
                        BindingOperations.GetBindingExpressionBase(control, property)?.Dispose();
                        // Observable 绑定不由 ClearValue 终止；用已完成的绑定替换，再释放它。
                        control.Bind(property, Observable.Empty<object>()).Dispose();
                        control.ClearValue(property);
                    }
                }
            }
        }
    }

    // FluentAvalonia 3.0 移除位图图标时未解除源的事件订阅；控件终结器又会释放共享图片。
    // 解除订阅后同时清空借用的引用，将图片的所有权留给图标源。
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UnlinkFromBitmapIconSource")]
    private static extern void UnlinkFromBitmapIconSource(FABitmapIcon icon);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_bitmap")]
    private static extern ref SKBitmap? GetBitmap(FABitmapIcon icon);

    private static Bitmap ConvertToMonochrome(Bitmap source, Color? monochromeColor)
    {
        var bitmap = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        try
        {
            using var framebuffer = bitmap.Lock();
            source.CopyPixels(framebuffer);
            var pixels = new byte[checked(framebuffer.RowBytes * framebuffer.Size.Height)];
            Marshal.Copy(framebuffer.Address, pixels, 0, pixels.Length);
            for (var y = 0; y < framebuffer.Size.Height; y++)
            {
                for (var x = 0; x < framebuffer.Size.Width; x++)
                {
                    var offset = y * framebuffer.RowBytes + x * 4;
                    // 灰度变换是线性的，可直接作用于预乘通道而不改变透明度。
                    var gray = (byte)((299 * pixels[offset + 2] + 587 * pixels[offset + 1] +
                                       114 * pixels[offset] + 500) / 1000);
                    if (monochromeColor is { } color)
                    {
                        var originalAlpha = pixels[offset + 3];
                        var alpha = (byte)((originalAlpha * color.A + 127) / 255);
                        pixels[offset] = TintChannel(color.B, gray, originalAlpha, alpha);
                        pixels[offset + 1] = TintChannel(color.G, gray, originalAlpha, alpha);
                        pixels[offset + 2] = TintChannel(color.R, gray, originalAlpha, alpha);
                        pixels[offset + 3] = alpha;
                        continue;
                    }
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = gray;
                }
            }
            Marshal.Copy(pixels, 0, framebuffer.Address, pixels.Length);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private static byte TintChannel(byte color, byte gray, byte originalAlpha, byte alpha)
    {
        if (originalAlpha == 0)
        {
            return 0;
        }
        // 按非预乘灰度在指定颜色与白色之间插值，再按输出透明度预乘。
        var denominator = originalAlpha * 255;
        return (byte)(((gray * 255 + (originalAlpha - gray) * color) * alpha + denominator / 2) / denominator);
    }
}
