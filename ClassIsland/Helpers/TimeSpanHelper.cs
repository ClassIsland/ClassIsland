using System;

namespace ClassIsland.Helpers;

public static class TimeSpanHelper
{
    public const double MaxTimeSpanSeconds = 2147483.0;

    public static TimeSpan FromSecondsSafe(double seconds)
    {
        return !double.IsRealNumber(seconds) ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Max(0, Math.Min(MaxTimeSpanSeconds, seconds)));
    }
}