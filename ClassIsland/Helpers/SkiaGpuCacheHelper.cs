using System;
using System.Globalization;
using ClassIsland.Core.Services;

namespace ClassIsland.Helpers;

internal static class SkiaGpuCacheHelper
{
    private const string StorageKey = "SkiaGpuCacheSizeMb";

    public const int DefaultSizeMb = 64;
    public const int MinimumSizeMb = 30;
    public const int MaximumSizeMb = 512;

    public static int SizeMb
    {
        get => int.TryParse(GlobalStorageService.GetValue(StorageKey), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out var value)
            ? Math.Clamp(value, MinimumSizeMb, MaximumSizeMb)
            : DefaultSizeMb;
        set => GlobalStorageService.SetValue(StorageKey,
            Math.Clamp(value, MinimumSizeMb, MaximumSizeMb).ToString(CultureInfo.InvariantCulture));
    }

    public static long SizeBytes => SizeMb * 1024L * 1024;
}
