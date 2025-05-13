using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.Windows.Media;
using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace ClassIsland.Helpers;

// https://raw.githubusercontent.com/Scighost/Starward/refs/heads/main/src/Starward/Helpers/AccentColorHelper.cs

internal static class AccentColorHelper
{

    public static unsafe Color? GetAccentColor(byte[] bgra, int width, int height)
    {
        if (bgra.Length % 4 == 0)
        {
            fixed (byte* ptr = bgra)
            {
                return GetAccentColorInternal(ptr, width, height);
            }
        }

        return null;
    }
    private static unsafe Color? GetAccentColorInternal(void* bgra, int width, int height)
    {
        try
        {
            uint* p = (uint*)bgra;
            long b = 0, g = 0, r = 0;
            int[] hueCircle = new int[360];
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    Bgra32 pixel = Unsafe.AsRef<Bgra32>(p);
                    b += pixel.B;
                    g += pixel.G;
                    r += pixel.R;
                    p += 2;
                }
                p += width - width % 2;
            }

            int c = (width / 2) * (height / 2);
            Unsafe.SkipInit(out Color color);
            color.B = (byte)(b / c);
            color.G = (byte)(g / c);
            color.R = (byte)(r / c);
            color.A = 255;
            HSV hsv = ColorConverter.RgbToHsv(new RGB(color.R, color.G, color.B));

            var result = ColorConverter.HsvToRgb(new HSV(hsv.H, 60, hsv.V));
            return Color.FromRgb(result.R, result.G, result.B);
        }
        catch { }
        return null;
    }


    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private readonly struct Bgra32
    {
        [FieldOffset(0)] public readonly byte B;
        [FieldOffset(1)] public readonly byte G;
        [FieldOffset(2)] public readonly byte R;
        [FieldOffset(3)] public readonly byte A;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Bgra32ToHue(in Bgra32 bgra)
    {
        byte max = Math.Max(Math.Max(bgra.R, bgra.G), bgra.B);
        byte min = Math.Min(Math.Min(bgra.R, bgra.G), bgra.B);
        float chroma = max - min;
        float h;

        if (chroma <= 8)
        {
            // ignore white black gray
            h = -1;
        }
        else if (max == bgra.R)
        {
            h = (((bgra.G - bgra.B) / chroma) + 6) % 6;
        }
        else if (max == bgra.G)
        {
            h = 2 + ((bgra.B - bgra.R) / chroma);
        }
        else
        {
            h = 4 + ((bgra.R - bgra.G) / chroma);
        }
        return (int)(h * 60);
    }


}