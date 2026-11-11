using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using ClassIsland.Platforms.Abstraction.Services;
using SkiaSharp;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace ClassIsland.Platform.Windows.Services;

public sealed class ScreenshotService : IScreenshotService
{
    private const uint SourceCopy = 0x00CC0020;
    private const uint CaptureLayeredWindows = 0x40000000;
    private const int Halftone = 4;

    public Task<ReadOnlyMemory<byte>?> CaptureWindowAsync(nint hwnd, CancellationToken cancellationToken = default) =>
        Task.Run<ReadOnlyMemory<byte>?>(() =>
        {
            if (CaptureWindow(hwnd, int.MaxValue) is not { } image)
                return null;
            return image;
        }, cancellationToken);

    public async IAsyncEnumerable<WindowScreenshot> EnumerateWindowsAsync(bool fullScreenOnly = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var candidates = await Task.Run(() => GetCandidates(fullScreenOnly, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var thumbnail = await Task.Run(() => CaptureWindow(candidate.Handle, 320), cancellationToken)
                .ConfigureAwait(false);
            yield return candidate with { ThumbnailPng = thumbnail ?? [] };
        }
    }

    internal static nint FindWindowByClass(string className, CancellationToken cancellationToken)
    {
        nint match = 0;
        VisitWindows(hwnd =>
        {
            if (!string.Equals(GetClassName(hwnd), className, StringComparison.Ordinal))
                return true;
            match = hwnd;
            return false;
        }, cancellationToken);
        return match;
    }

    internal static byte[]? CaptureWindow(nint hwnd, int maxSize)
    {
        if (hwnd == 0 || !TryGetWindowBounds(hwnd, out var bounds) || PInvoke.IsIconic((HWND)hwnd))
            return null;

        var dc = GetWindowDC(hwnd);
        return Capture(hwnd, dc, new PixelRect(0, 0, bounds.Width, bounds.Height), maxSize, SourceCopy);
    }

    internal static byte[]? CaptureScreen(PixelRect bounds) =>
        Capture(0, GetDC(0), bounds, 640, SourceCopy | CaptureLayeredWindows);

    private static byte[]? Capture(nint hwnd, nint source, PixelRect bounds, int maxSize, uint rasterOperation)
    {
        if (source == 0)
            return null;
        try
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return null;
            var scale = Math.Min(1d, (double)maxSize / Math.Max(bounds.Width, bounds.Height));
            var width = Math.Max(1, (int)Math.Round(bounds.Width * scale));
            var height = Math.Max(1, (int)Math.Round(bounds.Height * scale));
            if ((long)width * height > int.MaxValue / 4)
                return null;

            var destination = CreateCompatibleDC(source);
            if (destination == 0)
                return null;
            nint dib = 0;
            nint previous = 0;
            try
            {
                var header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = 32
                };
                dib = CreateDIBSection(destination, ref header, 0, out var pixels, 0, 0);
                if (dib == 0 || pixels == 0)
                    return null;
                previous = SelectObject(destination, dib);
                if (previous is 0 or -1)
                    return null;

                if (scale < 1)
                {
                    SetStretchBltMode(destination, Halftone);
                    SetBrushOrgEx(destination, 0, 0, 0);
                }
                if (!StretchBlt(destination, 0, 0, width, height, source,
                        bounds.X, bounds.Y, bounds.Width, bounds.Height, rasterOperation))
                    return null;
                GdiFlush();

                using var bitmap = new SKBitmap();
                var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
                if (!bitmap.InstallPixels(info, pixels, width * 4))
                    return null;
                using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                return png?.ToArray();
            }
            finally
            {
                if (previous is not (0 or -1))
                    SelectObject(destination, previous);
                if (dib != 0)
                    DeleteObject(dib);
                DeleteDC(destination);
            }
        }
        finally
        {
            ReleaseDC(hwnd, source);
        }
    }

    private static List<WindowScreenshot> GetCandidates(bool fullScreenOnly, CancellationToken cancellationToken)
    {
        var monitors = new List<PixelRect>();
        if (fullScreenOnly)
        {
            EnumDisplayMonitors(0, 0, (nint monitor, nint dc, ref RECT rect, nint data) =>
            {
                monitors.Add(ToPixelRect(rect));
                return true;
            }, 0);
        }

        var windows = new List<WindowScreenshot>();
        VisitWindows(hwnd =>
        {
            if (!PInvoke.IsWindowVisible((HWND)hwnd) || PInvoke.IsIconic((HWND)hwnd) ||
                !TryGetWindowBounds(hwnd, out var bounds) ||
                (fullScreenOnly && !monitors.Any(monitor => bounds.Contains(monitor))))
                return true;

            var className = GetClassName(hwnd);
            if (string.IsNullOrEmpty(className) || (fullScreenOnly && className == "WorkerW"))
                return true;

            using var title = new DisposablePWSTR(512);
            PInvoke.GetWindowText((HWND)hwnd, title.PWSTR, 512);
            windows.Add(new WindowScreenshot(hwnd, title.PWSTR.ToString(), className, bounds, default));
            return true;
        }, cancellationToken);
        return windows;
    }

    private static void VisitWindows(Func<nint, bool> visit, CancellationToken cancellationToken)
    {
        var keepGoing = true;
        bool Visit(nint hwnd) => keepGoing = !cancellationToken.IsCancellationRequested && visit(hwnd);
        EnumWindows((hwnd, _) =>
        {
            if (Visit(hwnd))
                EnumChildWindows(hwnd, (child, _) => Visit(child), 0);
            return keepGoing;
        }, 0);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static string GetClassName(nint hwnd)
    {
        using var name = new DisposablePWSTR(256);
        return PInvoke.GetClassName((HWND)hwnd, name.PWSTR, 256) > 0 ? name.PWSTR.ToString() : "";
    }

    private static bool TryGetWindowBounds(nint hwnd, out PixelRect bounds)
    {
        var found = PInvoke.GetWindowRect((HWND)hwnd, out RECT rect);
        bounds = ToPixelRect(rect);
        return found && bounds.Width > 0 && bounds.Height > 0;
    }

    private static PixelRect ToPixelRect(RECT rect) =>
        new(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

    private delegate bool WindowCallback(nint hwnd, nint data);
    private delegate bool MonitorCallback(nint monitor, nint dc, ref RECT bounds, nint data);

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPixelsPerMeter, YPixelsPerMeter;
        public uint ColorsUsed, ColorsImportant;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(WindowCallback callback, nint data);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(nint hwnd, WindowCallback callback, nint data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(nint dc, nint clip, MonitorCallback callback, nint data);

    [DllImport("user32.dll")]
    private static extern nint GetWindowDC(nint hwnd);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint hwnd, nint dc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint dc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateDIBSection(nint dc, ref BitmapInfoHeader info, uint usage, out nint pixels,
        nint section, uint offset);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint dc, nint obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(nint obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(nint dc);

    [DllImport("gdi32.dll")]
    private static extern bool GdiFlush();

    [DllImport("gdi32.dll")]
    private static extern bool StretchBlt(nint destination, int x, int y, int width, int height,
        nint source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, uint rasterOperation);

    [DllImport("gdi32.dll")]
    private static extern int SetStretchBltMode(nint dc, int mode);

    [DllImport("gdi32.dll")]
    private static extern bool SetBrushOrgEx(nint dc, int x, int y, nint previous);
}
