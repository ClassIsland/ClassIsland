using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Avalonia;
using ClassIsland.Platforms.Abstraction.Services;
using SkiaSharp;

namespace ClassIsland.Platforms.Linux.Services;

public sealed class DesktopAccentImageSource : IDesktopAccentImageSource
{
    private static readonly string ConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } path
        ? path
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

    public bool SupportsWallpaper => SupportsScreen;
    public bool SupportsScreen { get; } =
        !string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase) &&
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    public ValueTask<Stream?> CaptureWallpaperAsync(string windowClassName, bool compatibilityMode,
        CancellationToken cancellationToken) =>
        !SupportsWallpaper ? ValueTask.FromResult<Stream?>(null) : new(Task.Run(async () =>
        {
            var image = CaptureX11(null, cancellationToken);
            if (image != null)
                return image;
            var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
            var path = await ReadWallpaperPathAsync(desktop, cancellationToken).ConfigureAwait(false);
            return path != null ? File.OpenRead(path) : null;
        }, cancellationToken));

    public Stream? CaptureScreen(PixelRect bounds, CancellationToken cancellationToken) =>
        SupportsScreen ? CaptureX11(bounds, cancellationToken) : null;

    private static Stream? CaptureX11(PixelRect? screenBounds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var display = X.XOpenDisplay(null!);
        if (display == 0)
            return null;

        try
        {
            var root = X.XDefaultRootWindow(display);
            if (screenBounds is { } screen)
                return CaptureDrawable(display, root, screen, cancellationToken);

            foreach (var property in new[] { "_XROOTPMAP_ID", "ESETROOT_PMAP_ID" })
            {
                var pixmap = ReadPixmap(display, root, property);
                if (pixmap != 0 && CaptureDrawable(display, pixmap, null, cancellationToken) is { } image)
                    return image;
            }
            return null;
        }
        finally
        {
            X.XCloseDisplay(display);
        }
    }

    private static nint ReadPixmap(nint display, nint root, string name)
    {
        var atom = X.XInternAtom(display, name, true);
        if (atom == 0)
            return 0;
        var status = X.XGetWindowProperty(display, root, atom, 0, 1, false, X.XA_PIXMAP,
            out var type, out var format, out var count, out _, out var data);
        try
        {
            return status == 0 && type == X.XA_PIXMAP && format == 32 && count == 1 && data != 0
                ? Marshal.ReadIntPtr(data)
                : 0;
        }
        finally
        {
            if (data != 0)
                X.XFree(data);
        }
    }

    private static Stream? CaptureDrawable(nint display, nint drawable, PixelRect? screenBounds,
        CancellationToken cancellationToken)
    {
        if (X.XGetGeometry(display, drawable, out _, out _, out _, out var width, out var height, out _, out _) == 0)
            return null;
        var bounds = new PixelRect(0, 0, (int)width, (int)height);
        if (screenBounds is { } requested)
            bounds = bounds.Intersect(requested);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        var image = X.XGetImage(display, drawable, bounds.X, bounds.Y, (uint)bounds.Width, (uint)bounds.Height,
            nuint.MaxValue, X.ZPixmap);
        if (image == 0)
            return null;
        try
        {
            var layout = Marshal.PtrToStructure<X.XImageLayout>(image);
            var redMask = layout.RedMask;
            var greenMask = layout.GreenMask;
            var blueMask = layout.BlueMask;
            // XGetImage may leave the masks unset for a pixmap. The desktop pixmap uses the root visual.
            if ((redMask | greenMask | blueMask) == 0)
            {
                var visual = X.XDefaultVisual(display, X.XDefaultScreen(display));
                if (visual == 0)
                    return null;
                var rootVisual = Marshal.PtrToStructure<X.XVisualLayout>(visual);
                (redMask, greenMask, blueMask) = (rootVisual.RedMask, rootVisual.GreenMask, rootVisual.BlueMask);
            }
            if (redMask == 0 || greenMask == 0 || blueMask == 0)
                return null;

            var scale = Math.Min(1d, 640d / Math.Max(bounds.Width, bounds.Height));
            var targetWidth = Math.Max(1, (int)Math.Round(bounds.Width * scale));
            var targetHeight = Math.Max(1, (int)Math.Round(bounds.Height * scale));
            using var bitmap = new SKBitmap(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Opaque);
            var redShift = BitOperations.TrailingZeroCount(redMask);
            var greenShift = BitOperations.TrailingZeroCount(greenMask);
            var blueShift = BitOperations.TrailingZeroCount(blueMask);
            for (var y = 0; y < targetHeight; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceY = (int)((y + 0.5) * bounds.Height / targetHeight);
                for (var x = 0; x < targetWidth; x++)
                {
                    var sourceX = (int)((x + 0.5) * bounds.Width / targetWidth);
                    var pixel = X.XGetPixel(image, sourceX, sourceY);
                    bitmap.SetPixel(x, y, new SKColor(Expand(pixel, redMask, redShift),
                        Expand(pixel, greenMask, greenShift), Expand(pixel, blueMask, blueShift)));
                }
            }
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return data == null ? null : new MemoryStream(data.ToArray(), writable: false);
        }
        finally
        {
            X.XDestroyImage(image);
        }
    }

    private static byte Expand(nuint pixel, nuint mask, int shift) =>
        (byte)((ulong)((pixel & mask) >> shift) * 255 / (ulong)(mask >> shift));

    private static async Task<string?> ReadWallpaperPathAsync(string desktop, CancellationToken cancellationToken)
    {
        if (desktop.Contains("GNOME", StringComparison.OrdinalIgnoreCase))
        {
            var style = await ReadGSettingAsync("org.gnome.desktop.interface", "color-scheme", cancellationToken);
            const string schema = "org.gnome.desktop.background";
            if (style == "prefer-dark" &&
                LocalWallpaper(await ReadGSettingAsync(schema, "picture-uri-dark", cancellationToken)) is { } dark)
                return dark;
            return LocalWallpaper(await ReadGSettingAsync(schema, "picture-uri", cancellationToken));
        }

        if (desktop.Contains("KDE", StringComparison.OrdinalIgnoreCase) ||
            desktop.Contains("Plasma", StringComparison.OrdinalIgnoreCase))
            return ReadPlasmaWallpaper(cancellationToken);

        if (!desktop.Contains("XFCE", StringComparison.OrdinalIgnoreCase))
            return null;
        var xfce = Path.Combine(ConfigHome, "xfce4", "xfconf", "xfce-perchannel-xml", "xfce4-desktop.xml");
        return File.Exists(xfce)
            ? XDocument.Load(xfce).Descendants("property")
                .Where(node => (string?)node.Attribute("name") is "last-image" or "image-path")
                .Select(node => LocalWallpaper((string?)node.Attribute("value")))
                .FirstOrDefault(path => path != null)
            : null;
    }

    private static string? ReadPlasmaWallpaper(CancellationToken cancellationToken)
    {
        var config = Path.Combine(ConfigHome, "plasma-org.kde.plasma.desktop-appletsrc");
        if (!File.Exists(config))
            return null;
        var desktops = new Dictionary<string, (int Screen, string? Image)>();
        string[] group = [];
        foreach (var line in File.ReadLines(config))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.StartsWith('['))
            {
                group = line.Trim('[', ']').Split("][");
                continue;
            }
            if (group.Length < 2 || group[0] != "Containments")
                continue;
            var id = group[1];
            if (!desktops.TryGetValue(id, out var entry))
                entry = (-1, null);
            if (group.Length == 2 && line.StartsWith("lastScreen=", StringComparison.Ordinal) &&
                int.TryParse(line.AsSpan(11), out var screen))
                desktops[id] = (screen, entry.Image);
            if (group is [_, _, "Wallpaper", "org.kde.image", "General"] &&
                line.StartsWith("Image=", StringComparison.Ordinal))
                desktops[id] = (entry.Screen, line[6..]);
        }
        return desktops.Values.OrderBy(entry => entry.Screen < 0 ? int.MaxValue : entry.Screen)
            .Select(entry => LocalWallpaper(entry.Image)).FirstOrDefault(path => path != null);
    }

    private static string? LocalWallpaper(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var path = value;
        if (value.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.IsFile || !string.IsNullOrEmpty(uri.Host))
                return null;
            path = uri.LocalPath;
        }
        if (!Path.IsPathFullyQualified(path))
            return null;
        if (File.Exists(path))
            return path;

        var packageImages = Path.Combine(path, "contents", "images");
        return Directory.Exists(packageImages)
            ? Directory.EnumerateFiles(packageImages).OrderBy(file => file, StringComparer.Ordinal).FirstOrDefault()
            : null;
    }

    private static async Task<string?> ReadGSettingAsync(string schema, string key, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("gsettings") { RedirectStandardOutput = true, UseShellExecute = false };
        start.ArgumentList.Add("get");
        start.ArgumentList.Add(schema);
        start.ArgumentList.Add(key);
        try
        {
            using var process = Process.Start(start);
            if (process == null)
                return null;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            try
            {
                var output = await process.StandardOutput.ReadToEndAsync(timeout.Token).ConfigureAwait(false);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                return process.ExitCode == 0 ? output.Trim().Trim('\'') : null;
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    try { process.Kill(); }
                    catch (InvalidOperationException) { }
                }
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
